namespace BotSharp.Plugin.MessageQueue.Settings;

public class MessageQueueSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Enable the message queue consumers for delayed message handling
    /// </summary>
    public bool EnableConsumers { get; set; } = false;
}
