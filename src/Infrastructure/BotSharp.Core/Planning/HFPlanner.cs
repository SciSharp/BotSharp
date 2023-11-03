using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Planning;

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

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var completion = CompletionProvider.GetChatCompletion(_services);

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                var dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        MessageId = messageId
                    }
                };
                response = completion.GetChatCompletions(router, dialogs);

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

    public async Task<bool> AgentExecuting(FunctionCallFromLlm inst, RoleDialogModel message)
    {
        if (!string.IsNullOrEmpty(inst.AgentName))
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var agent = db.GetAgents(inst.AgentName).FirstOrDefault();

            var context = _services.GetRequiredService<RoutingContext>();
            context.Push(agent.Id);
        }

        return true;
    }

    public async Task<bool> AgentExecuted(FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var context = _services.GetRequiredService<RoutingContext>();
        context.Empty();
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "next_step_prompt.hf_planner").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(template, router.TemplateDict);
        return prompt.Trim();
    }
}
