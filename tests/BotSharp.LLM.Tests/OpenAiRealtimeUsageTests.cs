using System.Reflection;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Plugin.OpenAI.Providers.Realtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace BotSharp.LLM.Tests;

public class OpenAiRealtimeUsageTests
{
    private sealed class CapturingHook : IContentGeneratingHook
    {
        public TokenStatsModel? Stats;

        public Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
        {
            Stats = tokenStats;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ResponseDone_SubtractsCachedTokensFromInputTokens()
    {
        var hook = new CapturingHook();
        var services = new ServiceCollection()
            .AddSingleton<IContentGeneratingHook>(hook)
            .BuildServiceProvider();
        var provider = new RealTimeCompletionProvider(
            services, NullLogger<RealTimeCompletionProvider>.Instance, new BotSharpOptions());

        const string json = """
        {
          "type": "response.done",
          "response": {
            "status": "completed",
            "usage": {
              "input_token_details": {
                "text_tokens": 100,
                "audio_tokens": 50,
                "cached_tokens_details": { "text_tokens": 30, "audio_tokens": 20 }
              },
              "output_token_details": { "text_tokens": 7, "audio_tokens": 9 }
            },
            "output": []
          }
        }
        """;

        var method = typeof(RealTimeCompletionProvider).GetMethod(
            "OnResponsedDone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var conn = new RealtimeHubConnection { CurrentAgentId = "agent" };
        await (Task)method.Invoke(provider, [conn, json])!;

        hook.Stats.ShouldNotBeNull();
        hook.Stats!.TextInputTokens.ShouldBe(70);
        hook.Stats.CachedTextInputTokens.ShouldBe(30);
        hook.Stats.AudioInputTokens.ShouldBe(30);
        hook.Stats.CachedAudioInputTokens.ShouldBe(20);
    }
}
