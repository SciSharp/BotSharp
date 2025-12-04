using OpenAI;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.MMPEmbedding;

/// <summary>
/// Helper class to get the appropriate client based on provider type
/// Supports multiple providers: OpenAI, Azure OpenAI, DeepSeek, etc.
/// </summary>
public static class ProviderHelper
{
    /// <summary>
    /// Gets an OpenAI-compatible client based on the provider name
    /// </summary>
    /// <param name="provider">Provider name (e.g., "openai", "azure-openai")</param>
    /// <param name="model">Model name</param>
    /// <param name="services">Service provider for dependency injection</param>
    /// <returns>OpenAIClient instance configured for the specified provider</returns>
    public static OpenAIClient GetClient(string provider, string model, IServiceProvider services)
    {
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider, model);

        if (settings == null)
        {
            throw new InvalidOperationException($"Cannot find settings for provider '{provider}' and model '{model}'");
        }

        // Handle Azure OpenAI separately as it uses AzureOpenAIClient
        if (provider.Equals("azure-openai", StringComparison.OrdinalIgnoreCase))
        {
            return GetAzureOpenAIClient(settings);
        }

        // For OpenAI, DeepSeek, and other OpenAI-compatible providers
        return GetOpenAICompatibleClient(settings);
    }

    /// <summary>
    /// Gets an Azure OpenAI client
    /// </summary>
    private static OpenAIClient GetAzureOpenAIClient(LlmModelSetting settings)
    {
        if (string.IsNullOrEmpty(settings.Endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint is required");
        }

        var client = new AzureOpenAIClient(
            new Uri(settings.Endpoint),
            new ApiKeyCredential(settings.ApiKey)
        );

        return client;
    }

    /// <summary>
    /// Gets an OpenAI-compatible client (OpenAI, DeepSeek, etc.)
    /// </summary>
    private static OpenAIClient GetOpenAICompatibleClient(LlmModelSetting settings)
    {
        var options = !string.IsNullOrEmpty(settings.Endpoint)
            ? new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) }
            : null;

        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
    }
}
