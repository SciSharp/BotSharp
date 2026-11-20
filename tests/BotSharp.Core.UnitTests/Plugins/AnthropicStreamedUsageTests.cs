using System.Text.Json;
using Anthropic.SDK.Messaging;
using BotSharp.Plugin.AnthropicAI.Providers;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class AnthropicStreamedUsageTests
{
    [Fact]
    public void StreamingUsage_PreservesInputAndCacheAcrossEvents()
    {
        string[] events =
        [
            """{"message":{"usage":{"input_tokens":120,"cache_read_input_tokens":1000,"cache_creation_input_tokens":200,"output_tokens":1}}}""",
            """{"delta":{"text":"hello"}}""",
            """{"usage":{"output_tokens":8}}""",
            """{"delta":{"usage":{"output_tokens":12}}}""",
            """{"type":"message_stop"}"""
        ];
        Usage? usage = null;
        foreach (var json in events)
        {
            usage = ProviderHelper.UpdateStreamedUsage(usage, JsonSerializer.Deserialize<MessageResponse>(json)!);
        }

        Assert.NotNull(usage);
        Assert.Equal(120, usage.InputTokens);
        Assert.Equal(1000, usage.CacheReadInputTokens);
        Assert.Equal(200, usage.CacheCreationInputTokens);
        Assert.Equal(12, usage.OutputTokens);
    }

    [Fact]
    public void StreamingUsage_WithoutUsage_RemainsNull()
    {
        Assert.Null(ProviderHelper.UpdateStreamedUsage(null, new MessageResponse()));
    }

    [Fact]
    public void StreamingUsage_OutputOnly_IsRetained()
    {
        var response = JsonSerializer.Deserialize<MessageResponse>("""{"usage":{"output_tokens":9}}""")!;
        var usage = ProviderHelper.UpdateStreamedUsage(null, response);

        Assert.NotNull(usage);
        Assert.Equal(0, usage.InputTokens);
        Assert.Equal(9, usage.OutputTokens);
    }
}
