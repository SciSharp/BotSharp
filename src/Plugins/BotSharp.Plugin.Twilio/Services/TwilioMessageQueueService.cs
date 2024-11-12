using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
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

            // Clean static HttpContext
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>();
            httpContext.HttpContext = new DefaultHttpContext();
            httpContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            AssistantMessage reply = null;
            var inputMsg = new RoleDialogModel(AgentRole.User, message.Content);
            var conv = sp.GetRequiredService<IConversationService>();
            var routing = sp.GetRequiredService<IRoutingService>();
            var config = sp.GetRequiredService<TwilioSetting>();
            var sessionManager = sp.GetRequiredService<ITwilioSessionManager>();
            var progressService = sp.GetRequiredService<IConversationProgressService>();
            InitProgressService(message, sessionManager, progressService);
            InitConversation(message, inputMsg, conv, routing);
            
            var result = await conv.SendMessage(config.AgentId,
                inputMsg,
                replyMessage: BuildPostbackMessageModel(conv, message),
                async msg =>
                {
                    reply = new AssistantMessage()
                    {
                        ConversationEnd = msg.Instruction?.ConversationEnd ?? false,
                        HumanIntervationNeeded = string.Equals("human_intervention_needed", msg.FunctionName),
                        Content = msg.Content,
                        MessageId = msg.MessageId
                    };
                }
            );
            reply.SpeechFileName = await GetReplySpeechFileName(message.ConversationId, reply, sp);
            reply.Hints = GetHints(reply);
            reply.Content = null;
            await sessionManager.SetAssistantReplyAsync(message.ConversationId, message.SeqNumber, reply);
        }

        private PostbackMessageModel BuildPostbackMessageModel(IConversationService conv, CallerMessage message)
        {
            var messages = conv.GetDialogHistory(1);
            if (!messages.Any()) return null;
            var lastMessage = messages[0];
            if (string.IsNullOrEmpty(lastMessage.PostbackFunctionName)) return null;
            return new PostbackMessageModel
            {
                FunctionName = lastMessage.PostbackFunctionName,
                ParentId = lastMessage.MessageId,
                Payload = message.Digits
            };
        }

        private static void InitConversation(CallerMessage message, RoleDialogModel inputMsg, IConversationService conv, IRoutingService routing)
        {
            routing.Context.SetMessageId(message.ConversationId, inputMsg.MessageId);
            var states = new List<MessageState>
            {
                new("channel", ConversationChannel.Phone),
                new("channel_id", message.From),
                new("calling_phone", message.From)
            };
            states.AddRange(message.States.Select(kvp => new MessageState(kvp.Key, kvp.Value)));
            conv.SetConversationId(message.ConversationId, states);
        }

        private static async Task<string> GetReplySpeechFileName(string conversationId, AssistantMessage reply, IServiceProvider sp)
        {
            var completion = CompletionProvider.GetAudioCompletion(sp, "openai", "tts-1");
            var fileStorage = sp.GetRequiredService<IFileStorageService>();
            var data = await completion.GenerateAudioFromTextAsync(reply.Content);
            var fileName = $"reply_{reply.MessageId}.mp3";
            fileStorage.SaveSpeechFile(conversationId, fileName, data);
            return fileName;
        }

        private static string GetHints(AssistantMessage reply)
        {
            var phrases = reply.Content.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int capcity = 100;
            var hints = new List<string>(capcity);
            for (int i = phrases.Length - 1; i >= 0; i--)
            {
                var words = phrases[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int j = words.Length - 1; j >= 0; j--)
                {
                    hints.Add(words[j]);
                    if (hints.Count >= capcity)
                    {
                        break;
                    }
                }
                if (hints.Count >= capcity)
                {
                    break;
                }
            }
            // add frequency short words
            hints.AddRange(["yes", "no", "correct", "right"]);
            return string.Join(", ", hints.Select(x => x.ToLower()).Distinct().Reverse());
        }

        private static void InitProgressService(CallerMessage message, ITwilioSessionManager sessionManager, IConversationProgressService progressService)
        {
            progressService.OnFunctionExecuting = async msg =>
            {
                if (!string.IsNullOrEmpty(msg.Indication))
                {
                    await sessionManager.SetReplyIndicationAsync(message.ConversationId, message.SeqNumber, msg.Indication);
                }
            };
            progressService.OnFunctionExecuted = async msg => { };
        }
    }
}
