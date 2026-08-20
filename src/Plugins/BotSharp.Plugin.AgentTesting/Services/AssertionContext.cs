namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Everything an assertion is evaluated against. Turn-level and case-level share this shape and
/// differ only in how much of it is populated.
/// </summary>
public class AssertionContext
{
    public string? Output { get; set; }
    public IReadOnlyList<ObservedToolCall> ToolCalls { get; set; } = [];
    public IReadOnlyDictionary<string, string?> States { get; set; } = new Dictionary<string, string?>();
    public string? RoutedToAgent { get; set; }
}
