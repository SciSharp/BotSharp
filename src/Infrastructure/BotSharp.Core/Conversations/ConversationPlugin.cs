using BotSharp.Abstraction.Google.Settings;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Instructs;
using BotSharp.Core.Knowledges.Services;
using BotSharp.Core.Messaging;
using BotSharp.Core.Routing.Planning;
using BotSharp.Core.Templating;
using BotSharp.Core.Translation;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Conversations;

public class ConversationPlugin : IBotSharpPlugin
{
    public string Id => "99e9b971-a9f1-4273-84da-876d2873d192";
    public string Name => "Conversation";
    public string Description => "Provides conversations/ states management, saves dialogue logs, undo dialogs and channel access.";

    public SettingsMeta Settings =>
        new SettingsMeta("Conversation");
    public object GetNewSettingsInstance() =>
        new ConversationSetting();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<ConversationSetting>("Conversation");
        });

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<GoogleApiSettings>("GoogleApi");
        });

        services.AddScoped<IConversationStorage, ConversationStorage>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationStateService, ConversationStateService>();
        services.AddScoped<ITranslationService, TranslationService>();

        // Rich content messaging
        services.AddScoped<IRichContentService, RichContentService>();

        // Register template render
        services.AddSingleton<ITemplateRender, TemplateRender>();
        services.AddScoped<IResponseTemplateService, ResponseTemplateService>();

        services.AddScoped<IExecutor, InstructExecutor>();
        services.AddScoped<IInstructService, InstructService>();
        services.AddScoped<ITokenStatistics, TokenStatistics>();
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Apps");
        menu.Add(new PluginMenuDef("Conversation", link: "page/conversation", icon: "bx bx-conversation", weight: section.Weight + 5));
        return true;
    }
}
