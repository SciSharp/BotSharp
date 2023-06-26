using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using BotSharp.Plugin.MetaAI.Providers;
using BotSharp.Plugin.MetaAI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Plugin.MetaAI;

public class MetaAiPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MetaAiSettings();
        config.Bind("MetaAi", settings);
        services.AddSingleton(x => settings);
        services.AddSingleton(x => settings.fastText);

        services.AddSingleton<ITextEmbedding, fastTextEmbeddingProvider>();
        services.AddSingleton<IVectorDb, FaissDb>();
    }
}
