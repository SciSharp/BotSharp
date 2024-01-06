using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase;

public class KnowledgeBasePlugin : IBotSharpPlugin
{
    public string Name => "Knowledge Base";
    public string Description => "Embedding private data and feed them into LLM in the conversation.";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new KnowledgeBaseSettings();
        config.Bind("KnowledgeBase", settings);
        services.AddSingleton(x => settings);

        var a = config["KnowledgeBase"];

        services.AddScoped<ITextChopper, TextChopperService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddSingleton<IPdf2TextConverter, PigPdf2TextConverter>();
    }
}
