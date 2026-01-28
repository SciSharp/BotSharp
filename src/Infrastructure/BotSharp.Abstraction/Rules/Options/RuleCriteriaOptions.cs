using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleCriteriaOptions : CriteriaExecuteOptions
{
    /// <summary>
    /// Criteria execution provider
    /// </summary>
    public string Provider { get; set; } = "botsharp-rule";
}

public class CriteriaExecuteOptions
{
    /// <summary>
    /// Code processor provider
    /// </summary>
    public string? CodeProcessor { get; set; }

    /// <summary>
    /// Code script name
    /// </summary>
    public string? CodeScriptName { get; set; }

    /// <summary>
    /// Argument name as an input key to the code script
    /// </summary>
    public string? ArgumentName { get; set; }

    /// <summary>
    /// Json arguments as an input value to the code script
    /// </summary>
    public JsonDocument? ArgumentContent { get; set; }

    /// <summary>
    /// Custom parameters
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = [];
}