using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Planning;

public class FeedbackReasoningPlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public FeedbackReasoningPlanner(IServiceProvider services, ILogger<FeedbackReasoningPlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string conversation)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var content = $"{conversation}\r\n###\r\n{next}";

        var completion = CompletionProvider.GetChatCompletion(_services,
            model: "llm-gpt4");

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                response = completion.GetChatCompletions(router, new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, content)
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

    public async Task<bool> AgentExecuted(FunctionCallFromLlm inst, RoleDialogModel message)
    {
        inst.AgentName = null;
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
