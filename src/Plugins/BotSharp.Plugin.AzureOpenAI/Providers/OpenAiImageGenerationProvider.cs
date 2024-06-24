using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Logging;
using System;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class OpenAiImageGenerationProvider : ImageGenerationProvider
{
    public override string Provider => "openai";

    public OpenAiImageGenerationProvider(AzureOpenAiSettings settings,
        ILogger<OpenAiImageGenerationProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
    }
}
