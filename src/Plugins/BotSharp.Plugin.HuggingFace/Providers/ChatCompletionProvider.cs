using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Loggers;
using BotSharp.Plugin.HuggingFace.Services;
using BotSharp.Plugin.HuggingFace.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.HuggingFace.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "huggingface";

    private readonly IServiceProvider _services;
    private readonly HuggingFaceSettings _settings;
    private readonly ILogger _logger;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services,
        HuggingFaceSettings settings,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var prompt = agent.Instruction + "\r\n" + content;

        var api = _services.GetRequiredService<IInferenceApi>();

        var space = _model.Split('/')[0];
        var model = _model.Split("/")[1];

        var response = await api.TextGenerate(space, model, new InferenceInput
        {
            Inputs = prompt
        });

        var message = response[0].GeneratedText.Trim();

        var msg = new RoleDialogModel(AgentRole.Assistant, message)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        // Text response received
        await onMessageReceived(msg);

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        return true;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(agent);
        var prompt = instruction + "\r\n" + content;

        var api = _services.GetRequiredService<IInferenceApi>();

        var space = _model.Split('/')[0];
        var model = _model.Split("/")[1];

        var response = await api.TextGenerate(space, model, new InferenceInput
        {
            Inputs = prompt,
            Parameters = new InferenceInputParameters
            {
                MaxNewTokens = 64,
                Temperature = 0.7f
            }
        });

        var message = response[0].GeneratedText
            .Split($"{AgentRole.User}:")[0]
            .Split($"{AgentRole.Assistant}:")[0]
            .Trim();

        var msg = new RoleDialogModel(AgentRole.Assistant, message)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        foreach(var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return msg;
    }
}
