using BotSharp.Plugin.GoogleAi.Providers;
using GenerativeAI.Types;
using Xunit;

namespace BotSharp.Core.UnitTests.Plugins;

public class GoogleAiUsageTests
{
    [Fact]
    public void Output_tokens_include_thinking_tokens()
    {
        var usage = new UsageMetadata { CandidatesTokenCount = 20, ThoughtsTokenCount = 100 };

        Assert.Equal(120, ProviderHelper.GetOutputTokens(usage));
    }

    [Fact]
    public void Output_tokens_without_thinking_are_the_candidates_count()
    {
        Assert.Equal(20, ProviderHelper.GetOutputTokens(new UsageMetadata { CandidatesTokenCount = 20 }));
        Assert.Equal(0, ProviderHelper.GetOutputTokens(null));
    }
}
