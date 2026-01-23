using BotSharp.Plugin.RabbitMQ.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.RabbitMQ;

public class RabbitMQPlugin : IBotSharpAppPlugin
{
    public string Id => "3f93407f-3c37-4e25-be28-142a2da9b514";
    public string Name => "RabbitMQ";
    public string Description => "Handle AI messages in RabbitMQ.";
    public string IconUrl => "https://icon-library.com/images/message-queue-icon/message-queue-icon-13.jpg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new RabbitMQSettings();
        config.Bind("RabbitMQ", settings);
        services.AddSingleton(settings);

        var mqSettings = new MessageQueueSettings();
        config.Bind("MessageQueue", mqSettings);

        if (mqSettings.Enabled && mqSettings.Provider.IsEqualTo("RabbitMQ"))
        {
            services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
            services.AddSingleton<IMQService, RabbitMQService>();
        }
    }

    public void Configure(IApplicationBuilder app)
    {
#if DEBUG
        var sp = app.ApplicationServices;
        var mqSettings = sp.GetRequiredService<MessageQueueSettings>();

        if (mqSettings.Enabled && mqSettings.Provider.IsEqualTo("RabbitMQ"))
        {
            var mqService = sp.GetRequiredService<IMQService>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            // Create and subscribe the consumer using the abstract interface
            var scheduledConsumer = new ScheduledMessageConsumer(sp, loggerFactory.CreateLogger<ScheduledMessageConsumer>());
            mqService.SubscribeAsync(nameof(ScheduledMessageConsumer), scheduledConsumer)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            var dummyConsumer = new DummyMessageConsumer(sp, loggerFactory.CreateLogger<DummyMessageConsumer>());
            mqService.SubscribeAsync(nameof(DummyMessageConsumer), dummyConsumer)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
#endif
    }
}