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
    public string Name => "Meta AI";
    public string Description => "Innovating with the freedom to explore, discover and apply AI at scale.";
    public string IconUrl => "https://static.xx.fbcdn.net/rsrc.php/yJ/r/C1E_YZIckM5.svg";
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
