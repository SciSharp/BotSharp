using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.Anthropic.Settings;
using Anthropic.SDK;
using Anthropic.SDK.Completions;
using Anthropic.SDK.Constants;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Plugin.Anthropic.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "anthropic";
    private readonly IServiceProvider _services;
    private readonly AnthropicSettings _settings;
    private readonly ILogger _logger;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services,
        AnthropicSettings settings,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var client = new AnthropicClient(new APIAuthentication(_settings.Claude.ApiKey));

        var (prompt, hasFunctions) = PrepareOptions(agent, conversations);

        RoleDialogModel msg;

        var parameters = new SamplingParameters()
        {
            MaxTokensToSample = 256,
            Prompt = prompt,
            Temperature = 0.0m,
            StopSequences = new[] { AnthropicSignals.HumanSignal },
            Stream = false,
            Model = AnthropicModels.ClaudeInstant_v1_2
        };

        if (hasFunctions)
        {
            // use text completion
            // var response = client.GenerateTextAsync(prompt, null).Result;
            var response = client.Completions.GetClaudeCompletionAsync(parameters).Result;

            var message = response.Completion;

            // check if returns function calling
            var llmResponse = message.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = llmResponse.FunctionName,
                FunctionArgs = JsonSerializer.Serialize(llmResponse.Args)
            };
        }
        else
        {
            var response = client.Completions.GetClaudeCompletionAsync(parameters).Result;

            var message = response.Completion;

            // check if returns function calling
            var llmResponse = message.JsonContent<FunctionCallingResponse>();

            msg = new RoleDialogModel(llmResponse.Role, llmResponse.Content ?? message)
            {
                CurrentAgentId = agent.Id
            };
        }

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Model = _model
            })).ToArray());

        return msg;
    }

    private (string, bool) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var prompt = "";

        var agentService = _services.GetRequiredService<IAgentService>();

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            prompt += agentService.RenderedInstruction(agent);
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        var router = routing.Router;

        var hasFunctions = false;

        var render = _services.GetRequiredService<ITemplateRender>();
        var template = router.Templates.FirstOrDefault(x => x.Name == "response_with_function").Content;

        var response_with_function = render.Render(template, new Dictionary<string, object>
        {
            { "functions", agent.Functions },
            { "dialogs", conversations }
        });

        prompt += response_with_function;
        if (agent.Functions != null && agent.Functions.Count > 0)
        {
            hasFunctions = true;
        }

        if (conversations.Last().Role == AgentRole.Function)
        {
            // 4. Function calling result will be included in conversation, you can utilize the result to help make your decision.
            prompt += "\r\nHow to response based on the latest result from function, output your response in JSON.";
        }
        else
        {
            prompt += "\r\nWhat function or response should be used for the next step based on latest user request, output your response in JSON.";
        }

        return ($"{AnthropicSignals.HumanSignal} {prompt} {AnthropicSignals.AssistantSignal}", hasFunctions);
    }

    public Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
