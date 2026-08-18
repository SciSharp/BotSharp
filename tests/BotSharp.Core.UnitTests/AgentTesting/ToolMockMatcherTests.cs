using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// OneBrain 的工单流程里同一个查询函数在一次会话里被调好几次、每次参数不同，返回也必须不同。
/// 只按函数名选 mock，第一批真实用例就会撞上"三次调用拿到同一个假返回"。
/// 匹配优先级：入参子集匹配 > 调用序号 > 仅函数名，越具体越优先。
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
    /// ArgsMatchJson 自身含重复顶层键——语法上合法（JsonNode.Parse 不会报错），
    /// 但 System.Text.Json.Nodes.JsonObject 的底层字典是惰性物化的，第一次访问
    /// （foreach/TryGetPropertyValue）才会因为重复键抛 ArgumentException。
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
        // 入参来自模型输出，可能不是合法 JSON。这里必须降级，不能把整个用例炸成 Error。
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order", "{not json", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void Duplicate_key_in_the_actual_arguments_falls_back_to_the_name_only_mock_instead_of_throwing()
    {
        // 模型输出的实参里出现重复顶层键——JsonNode.Parse 本身不报错，但 IsSubset 里
        // actual.TryGetPropertyValue 第一次访问 actual 的字典时会抛 ArgumentException。
        // 必须降级到仅函数名的 mock，不能把整个用例炸成基础设施 Error。
        var hit = ToolMockMatcher.Match([ByName, ByArgs], "get_work_order",
            """{"woNum":"B9897413","woNum":"DUPLICATE"}""", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }

    [Fact]
    public void Duplicate_key_in_the_mocks_own_args_match_json_falls_back_to_the_name_only_mock_instead_of_throwing()
    {
        // 反过来：重复键出现在测试作者自己配的 ArgsMatchJson 里，崩溃点是 IsSubset 里
        // foreach (var (key, value) in expected) 第一次访问 expected 的字典。
        var hit = ToolMockMatcher.Match([ByName, ByArgsWithDuplicateKey], "get_work_order",
            """{"woNum":"B1"}""", 0);
        Assert.Equal("generic", hit!.ResultContent);
    }
}
