namespace BotSharp.Plugin.AgentSkills.Models;

/// <summary>
/// Represents an allowed tool specification from a skill's allowed-tools field.
/// Supports both exact matches and glob patterns.
/// </summary>
public sealed record AllowedTool
{
    /// <summary>
    /// The tool name or pattern (e.g., "read_file" or "execute_*").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this represents a glob pattern.
    /// </summary>
    public bool IsPattern { get; init; }

    private Regex? _regex;

    /// <summary>
    /// Checks if the given tool name matches this allowed tool specification.
    /// </summary>
    /// <param name="toolName">The tool name to check.</param>
    /// <returns>True if the tool name matches; otherwise, false.</returns>
    public bool Matches(string toolName)
    {
        if (!IsPattern)
        {
            return string.Equals(Name, toolName, StringComparison.OrdinalIgnoreCase);
        }

        _regex ??= new Regex(
            "^" + Regex.Escape(Name).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return _regex.IsMatch(toolName);
    }

    /// <summary>
    /// Parses a space-delimited allowed-tools string into individual <see cref="AllowedTool"/> instances.
    /// </summary>
    /// <param name="allowedToolsString">Space-delimited string of tool names/patterns.</param>
    /// <returns>Collection of parsed allowed tools.</returns>
    public static IReadOnlyList<AllowedTool> Parse(string? allowedToolsString)
    {
        if (string.IsNullOrWhiteSpace(allowedToolsString))
        {
            return [];
        }

        var tools = new List<AllowedTool>();
        var parts = allowedToolsString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var isPattern = part.Contains('*');
            tools.Add(new AllowedTool { Name = part, IsPattern = isPattern });
        }

        return tools;
    }
}
