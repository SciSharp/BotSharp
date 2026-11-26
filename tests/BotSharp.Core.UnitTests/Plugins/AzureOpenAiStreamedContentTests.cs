using BotSharp.Plugin.AzureOpenAI.Providers;
using OpenAI.Chat;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class AzureOpenAiStreamedContentTests
{
    [Theory]
    [InlineData("partial answer")]
    [InlineData("")]
    public void ResolveStreamedContent_Stop_ReturnsCollectedText(string text)
    {
        Assert.Equal(text, ProviderHelper.ResolveStreamedContent(ChatFinishReason.Stop, text));
    }

    // Streaming used to hand back the truncated text as if it were a complete answer; the non-streaming path shows a notice.
    [Theory]
    [InlineData("partial answer")]
    [InlineData("")]
    public void ResolveStreamedContent_Length_ReturnsSameNoticeAsNonStreaming(string text)
    {
        Assert.Equal("AI response exceeded max output length", ProviderHelper.ResolveStreamedContent(ChatFinishReason.Length, text));
    }
}
