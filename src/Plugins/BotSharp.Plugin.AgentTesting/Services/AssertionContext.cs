namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>断言求值的全部观测输入。轮级与整案级用同一个形状，只是填充范围不同。</summary>
public class AssertionContext
{
    public string? Output { get; set; }
    public IReadOnlyList<ObservedToolCall> ToolCalls { get; set; } = [];
    public IReadOnlyDictionary<string, string?> States { get; set; } = new Dictionary<string, string?>();
    public string? RoutedToAgent { get; set; }
}
