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

    [Fact]
    public void Cached_tokens_are_read_from_usage_metadata()
    {
        var usage = new UsageMetadata { PromptTokenCount = 1000, CachedContentTokenCount = 800 };

        Assert.Equal(800, ProviderHelper.GetCachedInputTokens(usage));
        Assert.Equal(0, ProviderHelper.GetCachedInputTokens(new UsageMetadata { PromptTokenCount = 10 }));
        Assert.Equal(0, ProviderHelper.GetCachedInputTokens(null));
    }

    [Fact]
    public void Text_input_tokens_exclude_cached_tokens()
    {
        // Gemini's PromptTokenCount already includes the cached part.
        var usage = new UsageMetadata { PromptTokenCount = 1000, CachedContentTokenCount = 800 };

        Assert.Equal(200, ProviderHelper.GetTextInputTokens(usage));
        Assert.Equal(10, ProviderHelper.GetTextInputTokens(new UsageMetadata { PromptTokenCount = 10 }));
        Assert.Equal(0, ProviderHelper.GetTextInputTokens(null));
    }
}
