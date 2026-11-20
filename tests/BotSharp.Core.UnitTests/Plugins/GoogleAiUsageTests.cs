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
    public void Cached_tokens_are_not_counted_as_fresh_input()
    {
        var usage = new UsageMetadata { PromptTokenCount = 1000, CachedContentTokenCount = 800 };

        Assert.Equal(200, ProviderHelper.GetInputTokens(usage));
        Assert.Equal(800, ProviderHelper.GetCachedInputTokens(usage));
    }

    [Fact]
    public void Input_tokens_without_cache_are_the_prompt_count()
    {
        Assert.Equal(50, ProviderHelper.GetInputTokens(new UsageMetadata { PromptTokenCount = 50 }));
        Assert.Equal(0, ProviderHelper.GetCachedInputTokens(new UsageMetadata { PromptTokenCount = 50 }));
        Assert.Equal(0, ProviderHelper.GetInputTokens(null));
        Assert.Equal(0, ProviderHelper.GetCachedInputTokens(null));
    }
}
