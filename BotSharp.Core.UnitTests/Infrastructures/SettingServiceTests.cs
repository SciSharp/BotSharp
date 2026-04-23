using BotSharp.Core.Infrastructures;
using Xunit;

namespace BotSharp.Core.UnitTests.Infrastructures;

public class SettingServiceTests
{    
    [Fact]
    public void Mask_null_or_empty_returns_empty()
    {
        Assert.Equal(string.Empty, SettingService.Mask(null!));
        Assert.Equal(string.Empty, SettingService.Mask(string.Empty));
    }

    [Theory]
    [InlineData("a", "*")]
    [InlineData("ab", "**")]
    [InlineData("abc", "a**")]
    [InlineData("abcd", "a***")]
    [InlineData("0123456789", "0123******")]
    public void Mask_short_and_medium_inputs_matches_expected_masked_form(string input, string expected)
    {
        var actual = SettingService.Mask(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Mask_long_value_preserves_length_and_replaces_suffix_with_stars()
    {
        var input = new string('x', 64);
        var masked = SettingService.Mask(input);

        Assert.Equal(64, masked.Length);
        Assert.NotEqual(input, masked);
        Assert.Contains('*', masked);
        Assert.StartsWith("x", masked, StringComparison.Ordinal);
        Assert.EndsWith("*", masked);
    }

    [Theory]
    [InlineData("e", 1)]
    [InlineData("ef", 2)]
    [InlineData("efg", 3)]
    [InlineData("efgh", 4)]
    [InlineData("123456789012345", 15)]
    [InlineData("abcdefghijklmnopqrstuvwxyz", 26)]
    public void Mask_preserves_original_string_length(string input, int expectedLength)
    {
        var masked = SettingService.Mask(input);
        Assert.Equal(expectedLength, masked.Length);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("password123")]
    public void Mask_contains_at_least_one_asterisk(string input)
    {
        var masked = SettingService.Mask(input);
        Assert.Contains('*', masked);
    }

    [Fact]
    public void Mask_very_long_string()
    {
        var input = new string('a', 1000);
        var masked = SettingService.Mask(input);
        
        Assert.Equal(1000, masked.Length);
        Assert.Contains('*', masked);
        var keepLength = (1000 - 1) / 2;
        var asteriskCount = 1000 - keepLength;
        Assert.Equal(asteriskCount, masked.Count(c => c == '*'));
    }

    [Fact]
    public void Mask_api_key_like_string()
    {
        var input = "sk_live_1234567890abcdef";
        var masked = SettingService.Mask(input);
        
        Assert.Equal(input.Length, masked.Length);
        Assert.Contains('*', masked);
        Assert.StartsWith("sk_li", masked);
    }
}
