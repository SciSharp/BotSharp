using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.MessageQueue;

public class MessageQueuePlugin : IBotSharpPlugin
{
    public string Id => "bac8bbf3-da91-4c92-98d8-db14d68e75ae";
    public string Name => "Message queue";
    public string Description => "Handle AI messages in queue.";
    public string IconUrl => "https://icon-library.com/images/message-queue-icon/message-queue-icon-13.jpg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MessageQueueSettings();
        config.Bind("MessageQueue", settings);
        services.AddSingleton(settings);
    }
}