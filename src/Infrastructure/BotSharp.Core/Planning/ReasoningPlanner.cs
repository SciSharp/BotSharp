using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Planning;

public class ReasoningPlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public ReasoningPlanner(IServiceProvider services, ILogger<ReasoningPlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var completion = CompletionProvider.GetChatCompletion(_services,
            model: "llm-gpt4");

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                response = completion.GetChatCompletions(router, new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                });

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
        message.Content = inst.Question;
        message.FunctionArgs = JsonSerializer.Serialize(inst.Arguments);
        
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgents(inst.AgentName).FirstOrDefault();

        var context = _services.GetRequiredService<RoutingContext>();
        context.Push(agent.Id);

        return true;
    }

    public async Task<bool> AgentExecuted(FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var context = _services.GetRequiredService<RoutingContext>();
        context.Pop();

        // push Router to continue
        // Make decision according to last agent's response

        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "next_step_prompt").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
        });
    }
}
