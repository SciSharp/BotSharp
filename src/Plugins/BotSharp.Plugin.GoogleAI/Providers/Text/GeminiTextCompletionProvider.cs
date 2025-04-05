using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Loggers;
using GenerativeAI;
using GenerativeAI.Core;

namespace BotSharp.Plugin.GoogleAi.Providers.Text;

public class GeminiTextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GeminiTextCompletionProvider> _logger;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;

    public string Provider => "google-ai";
    public string Model => _model;

    private GoogleAiSettings _settings;
    public GeminiTextCompletionProvider(
        IServiceProvider services,
        GoogleAiSettings googleSettings,
        ILogger<GeminiTextCompletionProvider> logger,
        ITokenStatistics tokenStatistics)
    {
        _settings = googleSettings;
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

        var client = ProviderHelper.GetGeminiClient(Provider, _model, _services);
        var aiModel = client.CreateGenerativeModel(_model);
        PrepareOptions(aiModel);

        var response = await aiModel.GenerateContentAsync(text);
        var completion = response?.Text ?? string.Empty;

        // After completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model,
                PromptCount = response.UsageMetadata?.PromptTokenCount ?? 0,
                CachedPromptCount = response.UsageMetadata?.CachedContentTokenCount ?? 0,
                CompletionCount = response.UsageMetadata?.CandidatesTokenCount ?? 0
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
        aiModel.FunctionCallingBehaviour = new FunctionCallingBehaviour()
        {
            AutoCallFunction = false
        };
    }
}
