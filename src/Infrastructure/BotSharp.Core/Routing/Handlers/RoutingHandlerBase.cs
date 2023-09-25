using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Templating;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Routing.Handlers;

public abstract class RoutingHandlerBase
{
    protected Agent _router;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected RoutingSettings _settings;
    protected List<RoleDialogModel> _dialogs;
    public virtual bool RequireAgent => true;

    public RoutingHandlerBase(IServiceProvider services,
        ILogger logger,
        RoutingSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public void SetRouter(Agent router)
    {
        _router = router;
    }

    public void SetDialogs(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
    }

    public async Task<FunctionCallFromLlm> GetNextInstructionFromReasoner(string prompt)
    {
        var responseFormat = JsonSerializer.Serialize(new FunctionCallFromLlm());
        var content = $"{prompt} Response must be in JSON format {responseFormat}";

        var chatCompletion = CompletionProvider.GetChatCompletion(_services,
            provider: _settings.Provider,
            model: _settings.Model);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(_router, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, content)
            }, async msg
            => response = msg, fn
            => Task.CompletedTask);

        var args = new FunctionCallFromLlm();
        try
        {
#if DEBUG
            Console.WriteLine(response.Content, Color.Gray);
#else
            _logger.LogInformation(response.Content);
#endif
            var pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
            response.Content = Regex.Match(response.Content, pattern).Value;
            args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);

            // Sometimes it populate malformed Function in Agent name
            if (args.Function == args.AgentName)
            {
                args.Function = "route_to_agent";
                _logger.LogWarning($"Captured LLM response ");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}: {response.Content}");
            args.Function = "response_to_user";
            args.Answer = ex.Message;
            args.AgentName = _settings.RouterName;
        }

        if (args.Arguments != null)
        {
            SaveStateByArgs(args.Arguments);
        }

        args.Function = args.Function.Split('.').Last();

#if DEBUG
        Console.WriteLine($"*** Next Instruction *** {args}", Color.Green);
#else
        _logger.LogInformation($"*** Next Instruction *** {args}");
#endif

        return args;
    }

    public async Task<RoleDialogModel> GetResponseFromReasoner()
    {
        var wholeDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, $"How to response to user?")
        };

        var chatCompletion = CompletionProvider.GetChatCompletion(_services,
            provider: _settings.Provider,
            model: _settings.Model);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(_router, wholeDialogs, async msg
            => response = msg, fn
            => Task.CompletedTask);

        return response;
    }

    const int MAXIMUM_RECURSION_DEPTH = 2;
    int CurrentRecursionDepth = 0;
    protected async Task<RoleDialogModel> InvokeAgent(string agentId)
    {
        CurrentRecursionDepth++;
        if (CurrentRecursionDepth > MAXIMUM_RECURSION_DEPTH)
        {
            return _dialogs.Last();
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, _dialogs,
            async msg =>
            {
                response = msg;
            }, async fn =>
            {
                // execute function
                // Save states
                SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(fn.FunctionArgs));

                var conversationService = _services.GetRequiredService<IConversationService>();
                // Call functions
                await conversationService.CallFunctions(fn);

                if (string.IsNullOrEmpty(fn.Content))
                {
                    fn.Content = fn.ExecutionResult;
                }

                _dialogs.Add(fn);

                if (!fn.StopCompletion)
                {
                    // Find response template
                    var templateService = _services.GetRequiredService<IResponseTemplateService>();
                    var quickResponse = await templateService.RenderFunctionResponse(agent.Id, fn);
                    if (!string.IsNullOrEmpty(quickResponse))
                    {
                        response = new RoleDialogModel(AgentRole.Assistant, quickResponse)
                        {
                            CurrentAgentId = agent.Id
                        };
                    }
                    else
                    {
                        response = await InvokeAgent(fn.CurrentAgentId);
                    }
                }
                else
                {
                    response = fn;
                }
            });

        return response;
    }

    protected void SaveStateByArgs(JsonDocument args)
    {
        if (args == null)
        {
            return;
        }

        var stateService = _services.GetRequiredService<IConversationStateService>();
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value);
                }
            }
        }
    }
}
