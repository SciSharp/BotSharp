using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
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
    public string? ArgsName { get; set; }

    /// <summary>
    /// Json arguments as an input value to the code script
    /// </summary>
    public JsonDocument? Arguments { get; set; }

    /// <summary>
    /// States
    /// </summary>
    public List<MessageState>? States { get; set; } = null;
}
