using BotSharp.Abstraction.Google.Settings;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Planning;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Instructs;
using BotSharp.Core.MessageHub;
using BotSharp.Core.MessageHub.Observers;
using BotSharp.Core.Messaging;
using BotSharp.Core.Routing.Reasoning;
using BotSharp.Core.Templating;
using BotSharp.Core.Translation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Conversations;

public class ConversationPlugin : IBotSharpPlugin, IBotSharpAppPlugin
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
            var render = provider.GetRequiredService<ITemplateRender>();
            render.RegisterType(typeof(ConversationSetting));
            return settingService.Bind<ConversationSetting>("Conversation");
        });

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<GoogleApiSettings>("GoogleApi");
        });

        services.AddSingleton<MessageHub<HubObserveData>>();

        services.AddScoped<IConversationStorage, ConversationStorage>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationProgressService, ConversationProgressService>();
        services.AddScoped<IConversationStateService, ConversationStateService>();
        services.AddScoped<ITranslationService, TranslationService>();

        // Rich content messaging
        services.AddScoped<IRichContentService, RichContentService>();

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

    public void Configure(IApplicationBuilder app)
    {
        var services = app.ApplicationServices;
        var queue = services.GetRequiredService<MessageHub<HubObserveData>>();
        var logger = services.GetRequiredService<ILogger<MessageHub<HubObserveData>>>();
        queue.Events.Subscribe(new ConversationObserver(logger));
    }
}
