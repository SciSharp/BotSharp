using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Planning;

public class NaivePlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public NaivePlanner(IServiceProvider services, ILogger<NaivePlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var next = GetNextStepPrompt(router);

        var inst = new FunctionCallFromLlm();

        // text completion
        /*var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(router);
        var content = $"{instruction}\r\n###\r\n{next}";
        content =  content + "\r\nResponse: ";
        var completion = CompletionProvider.GetTextCompletion(_services);*/

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        int retryCount = 0;
        while (retryCount < 3)
        {
            string text = string.Empty;
            try
            {
                // text completion
                // text = await completion.GetCompletion(content, router.Id, messageId);
                dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        FunctionName = nameof(NaivePlanner),
                        MessageId = messageId
                    }
                };
                var response = await completion.GetChatCompletions(router, dialogs);

                inst = response.Content.JsonContent<FunctionCallFromLlm>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {text}");
                inst.Function = "response_to_user";
                inst.Response = ex.Message;
                inst.AgentName = "Router";
            }
            finally
            {
                retryCount++;
            }
        }

        // Fix LLM malformed response
        PlannerHelper.FixMalformedResponse(_services, inst);

        return inst;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        // Set user content as Planner's question
        message.FunctionName = inst.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        if (inst.UnmatchedAgent)
        {
            var unmatchedAgentId = context.GetCurrentAgentId();

            // Exclude the wrong routed agent
            var agents = router.TemplateDict["routing_agents"] as RoutableAgent[];
            router.TemplateDict["routing_agents"] = agents.Where(x => x.AgentId != unmatchedAgentId).ToArray();

            // Handover to Router;
            context.Pop();
        }
        else
        {
            context.Empty(reason: $"Agent queue is cleared by {nameof(NaivePlanner)}");
            // context.Push(inst.OriginalAgent, "Push user goal agent");
        }
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.naive").Content;

        var states = _services.GetRequiredService<IConversationStateService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { StateConst.EXPECTED_ACTION_AGENT,  states.GetState(StateConst.EXPECTED_ACTION_AGENT) },
            { StateConst.EXPECTED_GOAL_AGENT,  states.GetState(StateConst.EXPECTED_GOAL_AGENT) }
        });
    }
}
