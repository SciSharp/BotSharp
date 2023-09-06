using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase;

public class KnowledgeBasePlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new KnowledgeBaseSettings();
        config.Bind("KnowledgeBase", settings);
        services.AddSingleton(x => settings);

        services.AddScoped<ITextChopper, TextChopperService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddSingleton<IPdf2TextConverter, PigPdf2TextConverter>();
    }
}
