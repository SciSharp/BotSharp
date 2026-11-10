using BotSharp.Plugin.AnthropicAI.Providers;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class AnthropicStreamedContentTests
{
    [Theory]
    [InlineData("partial answer")]
    [InlineData("")]
    public void ResolveStreamedContent_EndTurn_ReturnsCollectedText(string text)
    {
        Assert.Equal(text, ProviderHelper.ResolveStreamedContent("end_turn", text));
    }

    // Streaming used to surface the raw "max_tokens" string; the non-streaming path shows a notice.
    [Theory]
    [InlineData("partial answer")]
    [InlineData("")]
    public void ResolveStreamedContent_MaxTokens_ReturnsSameNoticeAsNonStreaming(string text)
    {
        Assert.Equal("AI response exceeded max output length", ProviderHelper.ResolveStreamedContent("max_tokens", text));
    }

    [Fact]
    public void ResolveStreamedContent_OtherStopReason_IsUnchanged()
    {
        Assert.Equal("refusal", ProviderHelper.ResolveStreamedContent("refusal", "x"));
    }
}
