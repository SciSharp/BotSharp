using BotSharp.Abstraction.Infrastructures.MessageQueues;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Messaging;

public class MessagingPlugin : IBotSharpPlugin
{
    public string Id => "52a0aa30-4820-42a9-9cae-df0be81bad2b";
    public string Name => "Messaging";
    public string Description => "Provides message queue services.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var mqSettings = new MessageQueueSettings();
        config.Bind("MessageQueue", mqSettings);
        services.AddSingleton(mqSettings);
    }
}