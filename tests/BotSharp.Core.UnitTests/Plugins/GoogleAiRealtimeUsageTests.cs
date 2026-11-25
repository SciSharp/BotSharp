using System.Reflection;
using System.Text.Json;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Plugin.GoogleAi.Providers.Realtime;
using BotSharp.Plugin.GoogleAi.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class GoogleAiRealtimeUsageTests
{
    [Fact]
    public async Task Completed_audio_response_reports_thoughts_as_text_output_tokens()
    {
        const string json = """
            {
              "serverContent": { "turnComplete": true, "outputTranscription": { "text": "Hello" } },
              "usageMetadata": {
                "responseTokenCount": 42,
                "responseTokensDetails": [{ "modality": "AUDIO", "tokenCount": 42 }],
                "thoughtsTokenCount": 17
              }
            }
            """;

        var hook = new UsageHook();
        using var services = new ServiceCollection()
            .AddSingleton<IContentGeneratingHook>(hook)
            .BuildServiceProvider();
        var provider = new GoogleRealTimeProvider(services, NullLogger<GoogleRealTimeProvider>.Instance, new GoogleAiSettings());
        var responseType = typeof(GoogleRealTimeProvider).Assembly
            .GetType("BotSharp.Plugin.GoogleAI.Models.Realtime.RealtimeServerResponse", throwOnError: true)!;
        var response = JsonSerializer.Deserialize(json, responseType);
        var usage = responseType.GetProperty("UsageMetaData")!.GetValue(response);
        var serverContent = responseType.GetProperty("ServerContent")!.GetValue(response)!;
        var transcription = serverContent.GetType().GetProperty("OutputTranscription")!.GetValue(serverContent)!;
        var text = transcription.GetType().GetProperty("Text")!.GetValue(transcription);
        var onResponseDone = typeof(GoogleRealTimeProvider)
            .GetMethod("OnResponseDone", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)onResponseDone.Invoke(provider, [new RealtimeHubConnection(), text, usage])!;

        var stats = Assert.IsType<TokenStatsModel>(hook.Stats);
        Assert.Equal(42, stats.AudioOutputTokens);
        Assert.Equal(17L, stats.TextOutputTokens);
    }

    private sealed class UsageHook : IContentGeneratingHook
    {
        public TokenStatsModel? Stats { get; private set; }

        public Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
        {
            Stats = tokenStats;
            return Task.CompletedTask;
        }
    }
}
