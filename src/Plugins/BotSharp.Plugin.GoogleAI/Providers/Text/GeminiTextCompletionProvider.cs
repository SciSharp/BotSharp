using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Loggers;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;

namespace BotSharp.Plugin.GoogleAi.Providers.Text;

public class GeminiTextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GeminiTextCompletionProvider> _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;

    public string Provider => "google-gemini";

    public GeminiTextCompletionProvider(
        IServiceProvider services,
        ILogger<GeminiTextCompletionProvider> logger,
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
        var agent = new Agent()
        {
            Id = agentId
        };
        var userMessage = new RoleDialogModel(AgentRole.User, text)
        {
            MessageId = messageId
        };

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, new List<RoleDialogModel> { userMessage });
        }

        var client = ProviderHelper.GetGeminiClient(_services);
        var aiModel = client.GenerativeModel(_model);
        PrepareOptions(aiModel);

        _tokenStatistics.StartTimer();
        var response = await aiModel.GenerateContent(text);
        _tokenStatistics.StopTimer();

        var completion = response.Text ?? string.Empty;

        // After completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model
            });
        }

        return completion;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }


    private void PrepareOptions(GenerativeModel aiModel)
    {
        var settings = _services.GetRequiredService<GoogleAiSettings>();
        aiModel.UseGoogleSearch = settings.Gemini.UseGoogleSearch;
        aiModel.UseGrounding = settings.Gemini.UseGrounding;
    }
}
