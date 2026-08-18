using System.Text.Json;
using System.Text.Json.Nodes;

namespace BotSharp.Plugin.AgentTesting.Runtime;

public static class ToolMockMatcher
{
    /// <summary>
    /// 选最具体的 mock：入参子集匹配 > 调用序号 > 仅函数名。
    /// 入参 JSON 来自模型输出，可能不合法；这里一律降级到不带入参条件的 mock，绝不抛异常
    /// （抛出会把用例记成基础设施 Error，掩盖真正的问题）。
    /// </summary>
    public static TestToolMock? Match(
        IReadOnlyList<TestToolMock> mocks,
        string functionName,
        string? argsJson,
        int callOrdinal)
    {
        var candidates = mocks
            .Where(m => string.Equals(m.FunctionName, functionName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var actual = ParseOrNull(argsJson);

        var byArgs = candidates.FirstOrDefault(m =>
            !string.IsNullOrWhiteSpace(m.ArgsMatchJson)
            && actual != null
            && IsSubset(ParseOrNull(m.ArgsMatchJson), actual));
        if (byArgs != null)
        {
            return byArgs;
        }

        var byOrdinal = candidates.FirstOrDefault(m => m.CallIndex == callOrdinal);
        if (byOrdinal != null)
        {
            return byOrdinal;
        }

        return candidates.FirstOrDefault(m =>
            string.IsNullOrWhiteSpace(m.ArgsMatchJson) && m.CallIndex == null);
    }

    /// <summary>
    /// public：AssertionEvaluator 的 toolCalled 分支复用这里对"顶层重复键"的物化修复，
    /// 而不是自己再写一个 JsonNode.Parse 包装（重复键的坑只该修一处）。
    /// </summary>
    public static JsonObject? ParseOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(json) as JsonObject;

            // JsonObject 的底层字典是惰性物化的：Parse 本身对重复顶层键不报错，
            // 直到第一次访问（foreach/TryGetPropertyValue/索引器）才抛 ArgumentException。
            // 这里主动强制物化一次，让"重复键"和"语法错误"在同一个 try 里被同样处理，
            // 而不是把异常留给调用方在 IsSubset 里意外撞到。
            _ = node?.Count;

            return node;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>expected 的每个键都在 actual 中存在且值的文本表示相等。</summary>
    public static bool IsSubset(JsonObject? expected, JsonObject actual)
    {
        if (expected == null)
        {
            return false;
        }

        foreach (var (key, value) in expected)
        {
            if (!actual.TryGetPropertyValue(key, out var actualValue))
            {
                return false;
            }

            if (value?.ToJsonString() != actualValue?.ToJsonString())
            {
                return false;
            }
        }

        return true;
    }
}
