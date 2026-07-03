using Microsoft.Agents.Authentication;
using Microsoft.Agents.Authentication.Msal;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;

namespace BotSharp.Plugin.MicrosoftTeams;

/// <summary>
/// Two-way Microsoft Teams integration built on Azure Bot Service / Microsoft 365 Agents SDK.
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

        // Build sub-config with Connections section required by Microsoft.Agents.Authentication.Msal.
        // Maps our MicrosoftTeamsSetting keys to the format ConfigurationConnections expects.
        var authConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Connections:BotServiceConnection:Assembly"] = "Microsoft.Agents.Authentication.Msal",
                ["Connections:BotServiceConnection:Type"] = "MsalAuth",
                ["Connections:BotServiceConnection:Settings:AuthType"] = "ClientSecret",
                ["Connections:BotServiceConnection:Settings:ClientId"] = settings.AppId,
                ["Connections:BotServiceConnection:Settings:ClientSecret"] = settings.AppPassword,
                ["Connections:BotServiceConnection:Settings:TenantId"] = settings.TenantId,
                ["Connections:BotServiceConnection:Settings:Scopes:0"] = "https://api.botframework.com/.default",
            })
            .Build();

        // Register MSAL credential provider used by RestChannelServiceClientFactory.
        services.AddDefaultMsalAuth(authConfig);

        // AddDefaultMsalAuth only registers the MSAL factory, not IConnections itself.
        // Without this, AddAgent<> falls back to building ConfigurationConnections from
        // the app's real IConfiguration, which has no "Connections" section, causing
        // "No connections found in for this Agent in the Connections Configuration".
        services.AddSingleton<IConnections>(sp => new ConfigurationConnections(sp, authConfig));

        // Register adapter + full SDK infrastructure (IChannelServiceClientFactory, IActivityTaskQueue,
        // background services, IAgentHttpAdapter, etc.) and TeamsActivityBot as default IAgent.
        services.AddAgent<TeamsActivityBot, TeamsAdapter>();

        // Expose adapter as IChannelAdapter so TeamsNotificationService can call ContinueConversationAsync.
        // AddAgent registers the adapter only as IAgentHttpAdapter, so resolve through that interface.
        services.AddSingleton<IChannelAdapter>(sp => (TeamsAdapter)sp.GetRequiredService<IAgentHttpAdapter>());

        services.AddSingleton<AdaptiveCardConverter>();
        services.AddSingleton<IConversationReferenceStore, InMemoryConversationReferenceStore>();
        services.AddSingleton<ITeamsNotificationService, TeamsNotificationService>();

        // Per-turn services.
        services.AddScoped<TeamsMessageHandler>();

        // Override IAgent lifetime to transient so each turn gets a fresh instance and
        // scoped dependencies (TeamsRequestState, TeamsMessageHandler) resolve correctly.
        services.AddTransient<IAgent, TeamsActivityBot>();
    }
}
