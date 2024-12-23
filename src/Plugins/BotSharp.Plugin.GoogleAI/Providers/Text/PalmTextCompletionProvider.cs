using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Loggers;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.GoogleAi.Providers.Text;

public class PalmTextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PalmTextCompletionProvider> _logger;
    private readonly ITokenStatistics _tokenStatistics;

    private string _model;

    public string Provider => "google-ai";

    public PalmTextCompletionProvider(
        IServiceProvider services,
        ILogger<PalmTextCompletionProvider> logger,
        ITokenStatistics tokenStatistics)
    {
        _services = services;
        _logger = logger;
        _tokenStatistics = tokenStatistics;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before completion hook
        var agent = new Agent() { Id = agentId };
        var userMessage = new RoleDialogModel(AgentRole.User, text) { MessageId = messageId };

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, new List<RoleDialogModel> { userMessage });
        }

        var client = ProviderHelper.GetPalmClient(_services);
        _tokenStatistics.StartTimer();
        var response = await client.GenerateTextAsync(text, null);
        _tokenStatistics.StopTimer();

        var message = response.Candidates.First();
        var completion = message.Output.Trim();

        // After completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider
            });
        }

        return completion;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
