using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

namespace BotSharp.Plugin.MicrosoftTeams;

/// <summary>
/// Two-way Microsoft Teams integration built on Azure Bot Service / Bot Framework.
/// Inbound: Teams activities are routed into the BotSharp conversation engine.
/// Outbound: proactive messages are pushed back via stored conversation references.
/// https://learn.microsoft.com/microsoftteams/platform/bots/what-are-bots
/// </summary>
public class MicrosoftTeamsPlugin : IBotSharpPlugin
{
    public string Id => "b6f8e1a2-2c4d-4e6f-8a91-7d3c5b9e0f12";
    public string Name => "Microsoft Teams";
    public string Description => "Two-way conversational integration with Microsoft Teams via Azure Bot Service.";
    public string IconUrl => "https://upload.wikimedia.org/wikipedia/commons/c/c9/Microsoft_Office_Teams_%282018%E2%80%93present%29.svg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MicrosoftTeamsSetting();
        config.Bind("MicrosoftTeams", settings);
        services.AddSingleton(settings);

        // Bot Framework authentication. ConfigurationBotFrameworkAuthentication expects the
        // canonical Microsoft* keys, so map our section onto them.
        var authConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftAppType"] = settings.AppType,
                ["MicrosoftAppId"] = settings.AppId,
                ["MicrosoftAppPassword"] = settings.AppPassword,
                ["MicrosoftAppTenantId"] = settings.TenantId
            })
            .Build();
        services.AddSingleton<BotFrameworkAuthentication>(sp =>
            new ConfigurationBotFrameworkAuthentication(authConfig));

        // Adapter (shared by inbound ProcessAsync and proactive ContinueConversationAsync).
        services.AddSingleton<TeamsAdapter>();
        services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetRequiredService<TeamsAdapter>());

        services.AddSingleton<AdaptiveCardConverter>();
        services.AddSingleton<IConversationReferenceStore, InMemoryConversationReferenceStore>();
        services.AddSingleton<ITeamsNotificationService, TeamsNotificationService>();

        // Per-request / per-turn services.
        services.AddScoped<TeamsRequestState>();
        services.AddScoped<TeamsMessageHandler>();
        services.AddTransient<IBot, TeamsActivityBot>();
    }
}
