using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Loggers;
using BotSharp.Plugin.Anthropic.Settings;

namespace BotSharp.Plugin.Anthropic.Providers;

public class TextCompletionProvider : ITextCompletion
{
    public string Provider => "anthropic";
    private readonly IServiceProvider _services;
    private readonly AnthropicSettings _settings;
    private readonly ILogger _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;

    public TextCompletionProvider(IServiceProvider services,
    AnthropicSettings settings,
    ILogger<TextCompletionProvider> logger,
    ITokenStatistics tokenStatistics)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
        _tokenStatistics = tokenStatistics;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        var agent = new Agent()
        {
            Id = agentId
        };
        var userMessage = new RoleDialogModel(AgentRole.User, text)
        {
            MessageId = messageId
        };
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, new List<RoleDialogModel> { userMessage })).ToArray());



        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, ""), new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return "";
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
