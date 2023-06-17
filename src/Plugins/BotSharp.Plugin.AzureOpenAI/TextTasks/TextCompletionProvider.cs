using Azure.AI.OpenAI;
using Azure;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Platform.AzureAi;
using System;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.TextTasks;

public class TextCompletionProvider : ITextCompletion
{
    private readonly AzureOpenAiSettings _settings;
    bool _useAzureOpenAI = true;

    public TextCompletionProvider(AzureOpenAiSettings settings)
    {
        _settings = settings;
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
            Temperature = 0.5f,
            MaxTokens = 128
        };

        var response = await client.GetCompletionsAsync(
            deploymentOrModelName: _settings.DeploymentName,
            completionsOptions);

        // OpenAI
        var completion = "";
        foreach (var t in response.Value.Choices)
        {
            completion += t.Text;
        };

        return completion;
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
