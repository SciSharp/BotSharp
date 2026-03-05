namespace BotSharp.Abstraction.Rules.Settings;

public class RuleSettings
{
    /// <summary>
    /// [type] => [providers], e.g., ["graph"] => ["graph provider 1", "graph provider 2"]
    /// </summary>
    public Dictionary<string, IEnumerable<string>> ConfigOptions { get; set; } = [];
}
