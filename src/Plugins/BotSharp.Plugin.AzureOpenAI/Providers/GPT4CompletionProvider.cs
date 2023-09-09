using Azure;
using Azure.AI.OpenAI;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Logging;
using System;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class GPT4CompletionProvider : ChatCompletionProvider
{
    private readonly AzureOpenAiSettings _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public override string ModelName => "gpt-4";

    public GPT4CompletionProvider(AzureOpenAiSettings settings, 
        ILogger<GPT4CompletionProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    protected override (OpenAIClient, string) GetClient()
    {
        var client = new OpenAIClient(new Uri(_settings.GPT4.Endpoint), new AzureKeyCredential(_settings.GPT4.ApiKey));
        return (client, _settings.GPT4.DeploymentModel);
    }
}
