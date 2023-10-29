using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Routing.Models;
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

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(router);
        var content = $"{instruction}\r\n###\r\n{next}";

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

        // Fix LLM malformed response
        FixMalformedResponse(inst);

        return inst;
    }

    public async Task<bool> AgentExecuting(FunctionCallFromLlm inst, RoleDialogModel message)
    {
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
        var template = router.Templates.First(x => x.Name == "next_step_prompt").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
        });
    }

    /// <summary>
    /// Sometimes LLM hallucinates and fails to set function names correctly.
    /// </summary>
    /// <param name="args"></param>
    private void FixMalformedResponse(FunctionCallFromLlm args)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = agentService.GetAgents(allowRouting: true).Result;
        var malformed = false;

        // Sometimes it populate malformed Function in Agent name
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function == args.AgentName)
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Another case of malformed response
        if (string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.Function))
        {
            args.AgentName = args.Function;
            args.Function = "route_to_agent";
            malformed = true;
        }

        // It should be Route to agent, but it is used as Response to user.
        if (!string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.AgentName) &&
            args.Function != "route_to_agent")
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Function name shouldn't contain dot symbol
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function.Contains('.'))
        {
            args.Function = args.Function.Split('.').Last();
            malformed = true;
        }

        if (malformed)
        {
            _logger.LogWarning($"Captured LLM malformed response");
        }
    }
}
