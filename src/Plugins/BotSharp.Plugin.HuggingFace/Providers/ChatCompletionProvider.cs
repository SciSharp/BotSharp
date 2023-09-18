using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Settings;
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
        var content = string.Join("\r\n", conversations.Select(x => $"{AgentRole.System}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var prompt = agent.Instruction + "\r\n" + content;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(prompt);
        }

        var api = _services.GetRequiredService<IInferenceApi>();

        if (_settings.Model.Contains('/'))
        {
            var space = _settings.Model.Split('/')[0];
            var model = _settings.Model.Split("/")[1];

            var response = await api.Post(space, model, new InferenceInput
            {
                Inputs = prompt
            });

            var falcon = JsonSerializer.Deserialize<List<FalconLlmResponse>>(response);

            var message = falcon[0].GeneratedText.Trim();
            _logger.LogInformation($"[{agent.Name}] {AgentRole.Assistant}: {message}");

            var msg = new RoleDialogModel(AgentRole.Assistant, message)
            {
                CurrentAgentId = agent.Id
            };

            // Text response received
            await onMessageReceived(msg);
        }

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        return true;
    }
}
