using Azure.AI.OpenAI;
using Azure;
using BotSharp.Abstraction.MLTasks;
using System;
using System.Threading.Tasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly AzureOpenAiSettings _settings;
    private readonly ILogger _logger;
    bool _useAzureOpenAI = true;
    private string _model;
    public string Provider => "azure-openai";

    public TextCompletionProvider(AzureOpenAiSettings settings, ILogger<TextCompletionProvider> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> GetCompletion(string text)
    {
        var client = GetOpenAIClient();
        var completionsOptions = new CompletionsOptions()
        {
            Prompts =
            {
                text
            },
            Temperature = 0.7f,
            MaxTokens = 256
        };

        var response = await client.GetCompletionsAsync(
            deploymentOrModelName: _settings.DeploymentModel.TextCompletionModel,
            completionsOptions);

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Choices)
        {
            completion += t.Text;
        };

        _logger.LogInformation(text + completion);

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private OpenAIClient GetOpenAIClient()
    {
        OpenAIClient client = _useAzureOpenAI
            ? new OpenAIClient(
               new Uri(_settings.Endpoint),
               new AzureKeyCredential(_settings.ApiKey))
            : new OpenAIClient("your-api-key-from-platform.openai.com");
        return client;
    }
}
