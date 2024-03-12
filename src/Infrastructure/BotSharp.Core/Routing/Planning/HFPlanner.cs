using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Planning;

/// <summary>
/// Human feedback based planner
/// </summary>
public class HFPlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public HFPlanner(IServiceProvider services, ILogger<HFPlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        FunctionName = nameof(HFPlanner),
                        MessageId = messageId
                    }
                };
                response = await completion.GetChatCompletions(router, dialogs);

                inst = response.Content.JsonContent<FunctionCallFromLlm>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {response.Content}");
                inst.Function = "response_to_user";
                inst.Response = ex.Message;
                inst.AgentName = "Router";
            }
            finally
            {
                retryCount++;
            }
        }

        return inst;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        if (!string.IsNullOrEmpty(inst.AgentName))
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var filter = new AgentFilter { AgentName = inst.AgentName };
            var agent = db.GetAgents(filter).FirstOrDefault();

            var context = _services.GetRequiredService<IRoutingContext>();
            context.Push(agent.Id);
        }

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        context.Empty(reason: $"Agent queue is cleared by {nameof(HFPlanner)}");
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.hf").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        // update states
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            router.TemplateDict[t.Key] = t.Value;
        }
        var prompt = render.Render(template, router.TemplateDict);
        return prompt.Trim();
    }
}
