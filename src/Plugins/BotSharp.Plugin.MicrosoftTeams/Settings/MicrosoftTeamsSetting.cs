namespace BotSharp.Plugin.MicrosoftTeams.Settings;

/// <summary>
/// Azure Bot Service / Bot Framework credentials.
/// https://learn.microsoft.com/azure/bot-service/bot-builder-authentication
/// </summary>
public class MicrosoftTeamsSetting
{
    /// <summary>
    /// MultiTenant | SingleTenant | UserAssignedMSI
    /// </summary>
    public string AppType { get; set; } = "MultiTenant";

    /// <summary>
    /// Azure Bot (App Registration) client id.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret. Keep it in Key Vault / environment variable, never in source control.
    /// Leave empty when AppType is UserAssignedMSI.
    /// </summary>
    public string AppPassword { get; set; } = string.Empty;

    /// <summary>
    /// Required for SingleTenant / UserAssignedMSI.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Default agent used by the proactive notification API when none is supplied.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to skip Azure Bot Service JWT validation on the inbound endpoint.
    /// Use this for local debugging with Agents Playground (emulator mode).
    /// Must be false in production.
    /// </summary>
    public bool AllowUnauthenticated { get; set; } = false;
}
