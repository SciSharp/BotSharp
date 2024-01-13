using BotSharp.Abstraction.Plugins.Models;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase;

public class KnowledgeBasePlugin : IBotSharpPlugin
{
    public string Id => "f1625e9f-8de5-467b-b230-556364d8b117";
    public string Name => "Knowledge Base";
    public string Description => "Embedding private data and feed them into LLM in the conversation.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/9592/9592995.png";
    public string[] AgentIds => new[] { "f5679799-ba89-4fef-936a-bcc311e5f14d" };

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

    public PluginMenuDef[] GetMenus()
    {
        return new PluginMenuDef[]
        {
            new PluginMenuDef("RAG", isHeader: true, weight: 20),
            new PluginMenuDef("Knowledge Base", link: "/page/knowledge-base", icon: "bx bx-book-open", weight: 21),
        };
    }
}
