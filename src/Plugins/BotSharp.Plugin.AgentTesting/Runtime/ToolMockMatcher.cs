using System.Text.Json;
using System.Text.Json.Nodes;

namespace BotSharp.Plugin.AgentTesting.Runtime;

public static class ToolMockMatcher
{
    /// <summary>
    /// Picks the most specific mock: argument-subset match beats call ordinal, which beats function
    /// name alone. The argument JSON comes from model output and may be malformed; this always
    /// degrades to a mock without argument conditions and never throws -- throwing would record the
    /// case as an infrastructure Error and hide the real problem.
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
    /// Public because AssertionEvaluator's toolCalled branch reuses the duplicate-top-level-key
    /// materialisation fix below rather than writing its own JsonNode.Parse wrapper -- that trap
    /// should be fixed in exactly one place.
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

            // JsonObject materialises its backing dictionary lazily: Parse itself does not
            // complain about duplicate top-level keys, and the ArgumentException only surfaces on
            // first access (foreach/TryGetPropertyValue/indexer). Forcing materialisation here puts
            // "duplicate keys" and "syntax error" through the same try block and the same handling,
            // rather than leaving the exception for a caller to hit unexpectedly inside IsSubset.
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

    /// <summary>
    /// Every key in expected is present in actual, with an equal textual representation of the value.
    /// </summary>
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
