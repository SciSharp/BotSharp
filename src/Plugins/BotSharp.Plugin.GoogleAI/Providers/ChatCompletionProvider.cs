using BotSharp.Abstraction.Agents;
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
    private string _model;

    public ChatCompletionProvider(IServiceProvider services, 
        GoogleAiSettings settings,
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

        var client = new GooglePalmClient(apiKey: _settings.PaLM.ApiKey);
        var messages = conversations.Select(c => new PalmChatMessage(c.Content, c.Role == AgentRole.User ? "user" : "AI"))
            .ToList();

        var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(agent);
        var response = client.ChatAsync(messages, instruction, null).Result;

        var message = response.Candidates.First();
        var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model
            })).ToArray());

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
