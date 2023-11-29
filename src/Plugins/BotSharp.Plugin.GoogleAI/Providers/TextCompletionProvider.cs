using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Loggers;
using BotSharp.Plugin.GoogleAI.Settings;
using LLMSharp.Google.Palm;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.GoogleAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    public string Provider => "google-ai";
    private readonly IServiceProvider _services;
    private readonly GoogleAiSettings _settings;
    private readonly ILogger _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;

    public TextCompletionProvider(IServiceProvider services,
    GoogleAiSettings settings,
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

        var client = new GooglePalmClient(apiKey: _settings.PaLM.ApiKey);
        _tokenStatistics.StartTimer();
        var response = await client.GenerateTextAsync(text, null);
        _tokenStatistics.StopTimer();

        var message = response.Candidates.First();
        var completion = message.Output.Trim();

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return completion;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
