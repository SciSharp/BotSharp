using BotSharp.Plugin.MessageQueue.Connections;
using BotSharp.Plugin.MessageQueue.Interfaces;
using BotSharp.Plugin.MessageQueue.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.MessageQueue;

public class MessageQueuePlugin : IBotSharpAppPlugin
{
    public string Id => "3f93407f-3c37-4e25-be28-142a2da9b514";
    public string Name => "Message Queue";
    public string Description => "Handle AI messages in RabbitMQ.";
    public string IconUrl => "https://icon-library.com/images/message-queue-icon/message-queue-icon-13.jpg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MessageQueueSettings();
        config.Bind("MessageQueue", settings);
        services.AddSingleton(settings);

        services.AddSingleton<IMQConnection, MQConnection>();
        services.AddSingleton<IMQService, MQService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        var sp = app.ApplicationServices;
        var mqConnection = sp.GetRequiredService<IMQConnection>();
        var mqService = sp.GetRequiredService<IMQService>();
        var logger = sp.GetRequiredService<ILogger<ScheduledMessageConsumer>>();

        mqService.Subscribe(nameof(ScheduledMessageConsumer), new ScheduledMessageConsumer(sp, mqConnection, logger));
    }
}