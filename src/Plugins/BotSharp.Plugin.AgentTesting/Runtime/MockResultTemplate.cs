using System.Text.Json;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// Fills <c>{{args.name}}</c> in a mock's result with that top-level argument of the call it answers.
/// </summary>
/// <remarks>
/// A real function that says back what the model passed it (a function that speaks a sentence the model wrote, with
/// StopCompletion) cannot be mocked with a fixed text: the turn's output would be the case author's words, and every
/// assertion on it would test the case, not the agent. A string argument is inserted as it is; any other JSON value as
/// its raw text; a missing argument, or arguments that are not a JSON object, as nothing.
/// </remarks>
public static partial class MockResultTemplate
{
    [GeneratedRegex(@"\{\{\s*args\.([A-Za-z0-9_\-]+)\s*\}\}")]
    private static partial Regex Placeholder();

    public static string Render(string template, string? functionArgs)
    {
        if (string.IsNullOrEmpty(template) || !template.Contains("{{")) return template;

        JsonElement? args = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(functionArgs) ? "{}" : functionArgs);
            if (doc.RootElement.ValueKind == JsonValueKind.Object) args = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
        }

        return Placeholder().Replace(template, match =>
        {
            if (args is not { } obj || !obj.TryGetProperty(match.Groups[1].Value, out var value)) return string.Empty;
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
        });
    }
}
