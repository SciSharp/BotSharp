using Microsoft.Agents.Authentication.Msal;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace BotSharp.Plugin.MicrosoftTeams;

/// <summary>
/// Two-way Microsoft Teams integration built on Azure Bot Service / Microsoft 365 Agents SDK.
/// Inbound: Teams activities are routed into the BotSharp conversation engine.
/// Outbound: proactive messages are pushed back via stored conversation references.
/// https://learn.microsoft.com/microsoftteams/platform/bots/what-are-bots
/// </summary>
public class MicrosoftTeamsPlugin : IBotSharpPlugin, IBotSharpAppPlugin
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
            })
            .Build();

        // Register MSAL credential provider (IConnections) used by RestChannelServiceClientFactory.
        services.AddDefaultMsalAuth(authConfig);

        // Register adapter + full SDK infrastructure (IChannelServiceClientFactory, IActivityTaskQueue,
        // background services, IAgentHttpAdapter, etc.) and TeamsActivityBot as default IAgent.
        services.AddAgent<TeamsActivityBot, TeamsAdapter>();

        // Expose adapter as IChannelAdapter so TeamsNotificationService can call ContinueConversationAsync.
        services.AddSingleton<IChannelAdapter>(sp => sp.GetRequiredService<TeamsAdapter>());

        services.AddSingleton<AdaptiveCardConverter>();
        services.AddSingleton<IConversationReferenceStore, InMemoryConversationReferenceStore>();
        services.AddSingleton<ITeamsNotificationService, TeamsNotificationService>();

        // Per-turn services.
        services.AddScoped<TeamsMessageHandler>();

        // Override IAgent lifetime to transient so each turn gets a fresh instance and
        // scoped dependencies (TeamsRequestState, TeamsMessageHandler) resolve correctly.
        services.AddTransient<IAgent, TeamsActivityBot>();
    }

    public void Configure(IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<MicrosoftTeamsSetting>();
        // Cast is safe — IApplicationBuilder in BotSharp is always WebApplication.
        if (app is IEndpointRouteBuilder router)
        {
            router.MapAgentApplicationEndpoints(requireAuth: !settings.AllowUnauthenticated, defaultPath: "/teams/api/messages");
        }
    }
}
