using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Plugin.GoogleAI.Settings;
using LLMSharp.Google.Palm;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.GoogleAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "google-ai";
    private readonly IServiceProvider _services;
    private readonly GoogleAiSettings _settings;
    private readonly ILogger _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services, 
        GoogleAiSettings settings,
        ILogger<ChatCompletionProvider> logger,
        ITokenStatistics tokenStatistics)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
        _tokenStatistics = tokenStatistics;
    }

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = new GooglePalmClient(apiKey: _settings.PaLM.ApiKey);
        List<PalmChatMessage> messages = new()
        {
            new(conversations.Last().Content, "user"),
        };
        _tokenStatistics.StartTimer();
        var response = client.ChatAsync(messages, agent.Instruction, null).Result;
        _tokenStatistics.StopTimer();

        var message = response.Candidates.First();
        var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
        {
            CurrentAgentId = agent.Id
        };

        return msg;
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
