using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using System.Drawing;

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
        var content = $"{prompt} Response must be in JSON format {responseFormat}.";

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

        FunctionCallFromLlm args = new FunctionCallFromLlm();
        try
        {
            _logger.LogInformation(response.Content);
            args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}: {response.Content}");
            args.Function = "response_to_user";
            args.Answer = ex.Message;
            args.Route.AgentName = "";
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

    protected async Task<RoleDialogModel> InvokeAgent(string agentId, List<RoleDialogModel> wholeDialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs,
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

                response = fn;

                if (string.IsNullOrEmpty(response.Content))
                {
                    response.Content = fn.ExecutionResult;
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
