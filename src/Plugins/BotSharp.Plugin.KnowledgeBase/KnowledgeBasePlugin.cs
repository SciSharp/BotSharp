using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.KnowledgeBase.Converters;
using BotSharp.Plugin.KnowledgeBase.Hooks;
using BotSharp.Plugin.KnowledgeBase.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase;

public class KnowledgeBasePlugin : IBotSharpPlugin
{
    public string Id => "f1625e9f-8de5-467b-b230-556364d8b117";
    public string Name => "Knowledge Base";
    public string Description => "Embedding private data and feed them into LLM in the conversation.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/9592/9592995.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<KnowledgeBaseSettings>("KnowledgeBase");
        });

        services.AddSingleton<IPdf2TextConverter, PigPdf2TextConverter>();
        services.AddScoped<IAgentUtilityHook, KnowledgeBaseUtilityHook>();
        services.AddScoped<IAgentHook, KnowledgeBaseAgentHook>();

        services.AddScoped<IKnowledgeService, KnowledgeService>();
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Knowledge Base", icon: "bx bx-book-open", weight: section.Weight + 1)
        {
            SubMenu = new List<PluginMenuDef>
            {
                new PluginMenuDef("Q & A", link: "page/knowledge-base/question-answer"),
                new PluginMenuDef("Relationships", link: "page/knowledge-base/relationships")
            }
        });
        return true;
    }
}
