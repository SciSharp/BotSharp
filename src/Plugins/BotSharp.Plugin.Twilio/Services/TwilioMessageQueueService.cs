using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.Extensions.Hosting;
using System;
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
            AssistantMessage reply = null;
            var inputMsg = new RoleDialogModel(AgentRole.User, message.Content);
            var conv = sp.GetRequiredService<IConversationService>();
            var routing = sp.GetRequiredService<IRoutingService>();
            var config = sp.GetRequiredService<TwilioSetting>();
            routing.Context.SetMessageId(message.ConversationId, inputMsg.MessageId);
            var states = new List<MessageState>
            {
                new MessageState("channel", ConversationChannel.Phone),
                new MessageState("calling_phone", message.From)
            };
            foreach (var kvp in message.States)
            {
                states.Add(new MessageState(kvp.Key, kvp.Value));
            }
            conv.SetConversationId(message.ConversationId, states);
            var sessionManager = sp.GetRequiredService<ITwilioSessionManager>();
            var result = await conv.SendMessage(config.AgentId,
                inputMsg,
                replyMessage: null,
                async msg =>
                {
                    reply = new AssistantMessage()
                    {
                        ConversationEnd = msg.Instruction?.ConversationEnd ?? false,
                        Content = msg.Content,
                        MessageId = msg.MessageId
                    };
                },
                async msg =>
                {
                    if (!string.IsNullOrEmpty(msg.Indication))
                    {
                        await sessionManager.SetReplyIndicationAsync(message.ConversationId, message.SeqNumber, msg.Indication);
                    }
                },
                async functionExecuted =>
                { }
            );
            var textToSpeechService = CompletionProvider.GetTextToSpeech(sp, "openai", "tts-1");
            var fileService = sp.GetRequiredService<IFileStorageService>();
            var data = await textToSpeechService.GenerateSpeechFromTextAsync(reply.Content);
            var fileName = $"reply_{reply.MessageId}.mp3";
            await fileService.SaveSpeechFileAsync(message.ConversationId, fileName, data);
            reply.SpeechFileName = fileName;
            reply.Content = null;
            await sessionManager.SetAssistantReplyAsync(message.ConversationId, message.SeqNumber, reply);
        }
    }
}
