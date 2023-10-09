using BotSharp.Abstraction.Conversations;
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

    public async Task<string> GetCompletion(string text)
    {
        var client = new GooglePalmClient(apiKey: _settings.PaLM.ApiKey);
        _tokenStatistics.StartTimer();
        var response = await client.GenerateTextAsync(text, null);
        _tokenStatistics.StopTimer();

        var message = response.Candidates.First();

        _logger.LogInformation(text);

        return message.Output.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
