using BotSharp.Plugin.GoogleAi.Providers;
using GenerativeAI.Types;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class GoogleAiStreamedContentTests
{
    // Streaming used to save FinishMessage, or the reason name when it was absent.
    // The non-streaming path shows a notice for the same MAX_TOKENS case.
    [Theory]
    [InlineData("partial answer")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveStreamedContent_MaxTokens_ReturnsSameNoticeAsNonStreaming(string? finishMessage)
    {
        Assert.Equal("AI response exceeded max output length",
            ProviderHelper.ResolveStreamedContent(FinishReason.MAX_TOKENS, finishMessage));
    }

    [Theory]
    [InlineData("blocked by safety")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveStreamedContent_OtherReason_KeepsFinishMessageOrReasonName(string? finishMessage)
    {
        var expected = finishMessage ?? FinishReason.SAFETY.ToString();
        Assert.Equal(expected, ProviderHelper.ResolveStreamedContent(FinishReason.SAFETY, finishMessage));
    }
}
