using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Planning;

public class NaivePlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public NaivePlanner(IServiceProvider services, ILogger<NaivePlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string conversation)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(router);
        var content = $"{instruction}\r\n{conversation}\r\n###\r\n{next}";

        // text completion
        content =  content + "\r\nResponse: ";

        var completion = CompletionProvider.GetTextCompletion(_services);

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                var text = await completion.GetCompletion(content);
                response = new RoleDialogModel(AgentRole.Assistant, text);
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
