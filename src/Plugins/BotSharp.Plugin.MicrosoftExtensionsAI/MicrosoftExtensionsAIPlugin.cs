using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.MicrosoftExtensionsAI;

/// <summary>
/// Use Microsoft.Extensions.AI as BotSharp plugin
/// </summary>
public sealed class MicrosoftExtensionsAIPlugin : IBotSharpPlugin
{
    /// <inheritdoc/>
    public string Id => "B7F2AB8D-1BBA-41CE-9642-2D5E6B5F86A0";

    /// <inheritdoc/>
    public string Name => "Microsoft.Extensions.AI";

    /// <inheritdoc/>
    public string Description => "Microsoft.Extensions.AI Service";

    /// <inheritdoc/>
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextCompletion, MicrosoftExtensionsAITextCompletionProvider>();
        services.AddScoped<IChatCompletion, MicrosoftExtensionsAIChatCompletionProvider>();
        services.AddScoped<ITextEmbedding, MicrosoftExtensionsAITextEmbeddingProvider>();
    }
}