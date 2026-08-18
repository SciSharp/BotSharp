using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// In a real work order flow the same lookup function is called several times in one conversation
/// with different arguments, and each call has to return something different. Selecting a mock by
/// function name alone means the very first batch of real cases hits "three calls, one fake return".
/// Match priority, most specific first: argument-subset match, then call ordinal, then function name
/// alone.
/// </summary>
public class ToolMockMatcherTests
{
    private static readonly TestToolMock ByName = new()
    {
        FunctionName = "get_work_order",
        ResultContent = "generic"
    };

    private static readonly TestToolMock ByArgs = new()
    {
        FunctionName = "get_work_order",
        ArgsMatchJson = """{"woNum":"B9897413"}""",
        ResultContent = "specific"
    };

    private static readonly TestToolMock ByOrdinal = new()
    {
        FunctionName = "get_work_order",
        CallIndex = 1,
        ResultContent = "second-call"
    };

    /// <summary>
    /// ArgsMatchJson itself contains duplicate top-level keys -- syntactically valid, since
    /// JsonNode.Parse does not object, but System.Text.Json.Nodes.JsonObject materialises its
    /// backing dictionary lazily and the duplicate only throws ArgumentException on first access
    /// (foreach/TryGetPropertyValue).
    /// </summary>
    private static readonly TestToolMock ByArgsWithDuplicateKey = new()
    {
        FunctionName = "get_work_order",
        ArgsMatchJson = """{"woNum":"B1","woNum":"B2"}""",
        ResultContent = "should-never-match"
    };

    [Fact]
    public void Matches_on_function_name_when_nothing_more_specific_is_configured()
    {
        var hit = ToolMockMatcher.Match([ByName], "get_work_order", null, 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void An_args_subset_match_beats_a_name_only_mock()
    {
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order",
            """{"woNum":"B9897413","includeNotes":true}""", 0);
        Assert.Equal("specific", hit!.ResultContent);
    }

    [Fact]
    public void An_args_mock_does_not_match_different_arguments()
    {
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order", """{"woNum":"OTHER"}""", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void Call_index_selects_a_different_mock_for_a_later_call()
    {
        Assert.Equal("generic", ToolMockMatcher.Match([ByName, ByOrdinal], "get_work_order", null, 0)!.ResultContent);
        Assert.Equal("second-call", ToolMockMatcher.Match([ByName, ByOrdinal], "get_work_order", null, 1)!.ResultContent);
    }

    [Fact]
    public void Returns_null_when_the_function_has_no_mock_at_all()
    {
        Assert.Null(ToolMockMatcher.Match([ByName], "send_text_message", null, 0));
    }

    [Fact]
    public void Malformed_argument_json_falls_back_to_the_name_only_mock_instead_of_throwing()
    {
        // The arguments come from model output and may not be valid JSON. This has to degrade
        // gracefully rather than blowing the whole case up into an Error.
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order", "{not json", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void Duplicate_key_in_the_actual_arguments_falls_back_to_the_name_only_mock_instead_of_throwing()
    {
        // Duplicate top-level keys in the model's own arguments: JsonNode.Parse does not object,
        // but actual.TryGetPropertyValue inside IsSubset throws ArgumentException the first time it
        // touches actual's dictionary. This has to fall back to the function-name-only mock rather
        // than blowing the whole case up into an infrastructure Error.
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order",
            """{"woNum":"B9897413","woNum":"DUPLICATE"}""", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void Duplicate_key_in_the_mocks_own_args_match_json_falls_back_to_the_name_only_mock_instead_of_throwing()
    {
        // The mirror case: the duplicate key is in the ArgsMatchJson the test author wrote, and the
        // throw happens where IsSubset's foreach first touches expected's dictionary.
        var hit = ToolMockMatcher.Match([ByName, ByArgsWithDuplicateKey], "get_work_order",
            """{"woNum":"B1"}""", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }
}
