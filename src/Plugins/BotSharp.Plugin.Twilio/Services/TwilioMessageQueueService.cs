using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services
{
    public class TwilioMessageQueueService : BackgroundService
    {
        private readonly TwilioMessageQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _throttler;

        public TwilioMessageQueueService(
            TwilioMessageQueue queue,
            IServiceProvider serviceProvider)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _throttler = new SemaphoreSlim(4, 4);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await _throttler.WaitAsync(stoppingToken);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"Start processing {message}.");
                        await ProcessUserMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Processing {message} failed due to {ex.Message}.");
                    }
                    finally
                    {
                        _throttler.Release();
                    }
                });
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _queue.Stop();
            await base.StopAsync(cancellationToken);
        }

        private async Task ProcessUserMessageAsync(CallerMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            string reply = null;
            var inputMsg = new RoleDialogModel(AgentRole.User, message.Content);
            var conv = sp.GetRequiredService<IConversationService>();
            var routing = sp.GetRequiredService<IRoutingService>();
            var config = sp.GetRequiredService<TwilioSetting>();
            routing.Context.SetMessageId(message.SessionId, inputMsg.MessageId);
            conv.SetConversationId(message.SessionId, new List<MessageState>
            {
                new MessageState("channel", ConversationChannel.Phone),
                new MessageState("calling_phone", message.From)
            });
            var result = await conv.SendMessage(config.AgentId,
                inputMsg,
                replyMessage: null,
                async msg =>
                {
                    reply = msg.Content;
                },
                async functionExecuting =>
                { },
                async functionExecuted =>
                { }
            );
            if (string.IsNullOrWhiteSpace(reply))
            {
                reply = "Sorry, something was wrong.";
            }
            var sessionManager = sp.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.SetAssistantReplyAsync(message.SessionId, message.SeqNumber, reply);
        }
    }
}
