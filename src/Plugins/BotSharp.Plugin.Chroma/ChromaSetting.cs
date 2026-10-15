namespace BotSharp.Plugin.Chroma;

public class ChromaSetting
{
    /// <summary>
    /// Server address, like http://localhost:8000, or https://api.trychroma.com for Chroma Cloud
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// API key of Chroma Cloud, or the token of a server with token authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Tenant: on Chroma Cloud the API key gives it when empty, otherwise default_tenant
    /// </summary>
    public string Tenant { get; set; } = string.Empty;

    /// <summary>
    /// Database: on Chroma Cloud an API key for one database gives it when empty, otherwise default_database
    /// </summary>
    public string Database { get; set; } = string.Empty;
}
