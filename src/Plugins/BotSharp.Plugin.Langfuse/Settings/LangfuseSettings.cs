namespace BotSharp.Plugin.Langfuse.Settings;

public class LangfuseSettings
{
    /// <summary>
    /// Whether Langfuse observability is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Langfuse public key
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Langfuse secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Langfuse host URL
    /// </summary>
    public string Host { get; set; } = "https://cloud.langfuse.com";

    /// <summary>
    /// Whether to log conversation details
    /// </summary>
    public bool LogConversations { get; set; } = true;

    /// <summary>
    /// Whether to log function executions
    /// </summary>
    public bool LogFunctions { get; set; } = true;

    /// <summary>
    /// Whether to log token usage statistics
    /// </summary>
    public bool LogTokenStats { get; set; } = true;

    /// <summary>
    /// Session timeout in seconds
    /// </summary>
    public int SessionTimeoutSeconds { get; set; } = 3600;
}