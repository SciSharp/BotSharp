using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Templating;
using System.Drawing;

namespace BotSharp.Core.Routing.Planning;

public class SequentialPlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public bool HideDialogContext => true;
    public int MaxLoopCount => 100;
    private FunctionCallFromLlm _lastInst;

    public SequentialPlanner(IServiceProvider services, ILogger<NaivePlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var decomposation = await GetDecomposedStepAsync(router, messageId, dialogs);
        if (decomposation.TotalRemainingSteps > 0 && _lastInst != null)
        {
            _lastInst.Response = decomposation.Description;
            _lastInst.Reason = $"{decomposation.TotalRemainingSteps} left.";
            return _lastInst;
        }

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
                        FunctionName = nameof(SequentialPlanner),
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

        if (decomposation.TotalRemainingSteps > 0)
        {
            inst.Response = decomposation.Description;
            inst.Reason = $"{decomposation.TotalRemainingSteps} steps left.";
            inst.HideDialogContext = true;
        }

        _lastInst = inst;
        return inst;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        // Set user content as Planner's question
        message.FunctionName = inst.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var context = _services.GetRequiredService<RoutingContext>();

        if (message.StopCompletion)
        {
            context.Empty();
            return false;
        }

        // Handover to Router;
        context.Pop();

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.ResetRecursiveCounter();

        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.sequential").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
        });
    }

    public async Task<DecomposedStep> GetDecomposedStepAsync(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var systemPrompt = GetDecomposeTaskPrompt(router);

        var inst = new DecomposedStep();

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            model: "llm-gpt4");

        int retryCount = 0;
        while (retryCount < 2)
        {
            string text = string.Empty;
            try
            {
                var response = await completion.GetChatCompletions(new Agent
                {
                    Id = router.Id,
                    Name = nameof(SequentialPlanner),
                    Instruction = systemPrompt
                }, dialogs);

                text = response.Content;
                Console.WriteLine(text, Color.OrangeRed);
                inst = response.Content.JsonContent<DecomposedStep>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {text}");
            }
            finally
            {
                retryCount++;
            }
        }

        return inst;
    }

    private string GetDecomposeTaskPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.sequential.get_remaining_task").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
        });
    }
}
