using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services;

public class TwilioMessageQueueService : BackgroundService
{
    private readonly TwilioMessageQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _throttler;
    private readonly ILogger _logger;

    public TwilioMessageQueueService(
        TwilioMessageQueue queue,
        IServiceProvider serviceProvider,
        ILogger<TwilioMessageQueueService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _throttler = new SemaphoreSlim(20, 20);
        _logger = logger;
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
                    _logger.LogInformation($"Start processing {message}.");
                    await ProcessUserMessageAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Processing {message} failed due to {ex.Message}.");
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
        var sessionManager = sp.GetRequiredService<ITwilioSessionManager>();

        var reply = await TwilioConversationProcessor.SendCallerMessageAsync(sp, message, new AgentTurnCallbacks
        {
            OnIndication = async indication => await sessionManager.SetReplyIndicationAsync(message.ConversationId, message.SeqNumber, indication)
        });
        if (reply == null)
        {
            _logger.LogWarning($"No reply for {message}.");
            return;
        }

        var settings = sp.GetRequiredService<TwilioSetting>();
        if (settings.GenerateReplyAudio)
        {
            reply.SpeechFileName = await GetReplySpeechFileName(message.ConversationId, reply, sp);
        }

        reply.Hints = await GetHints(message.AgentId, reply, sp);
        await sessionManager.SetAssistantReplyAsync(message.ConversationId, message.SeqNumber, reply);
    }

    private static async Task<string> GetReplySpeechFileName(string conversationId, AssistantMessage reply, IServiceProvider sp)
    {
        var twilio = sp.GetRequiredService<TwilioService>();
        return await twilio.SaveSpeechAsync(conversationId, $"reply_{reply.MessageId}", reply.Content);
    }

    private static async Task<string> GetHints(string agentId, AssistantMessage reply, IServiceProvider sp)
    {
        var agentService = sp.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        var extraWords = new List<string>();
        HookEmitter.Emit<IRealtimeHook>(sp, hook => extraWords.AddRange(hook.OnModelTranscriptPrompt(agent)),
            agentId);

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
        hints.AddRange(extraWords);
        return string.Join(", ", hints.Select(x => x.ToLower()).Distinct().Reverse());
    }
}
