using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Code processor provider
    /// </summary>
    public string? CodeProcessor { get; set; } = "botsharp-py-interpreter";

    /// <summary>
    /// Code script name
    /// </summary>
    public string? CodeScriptName { get; set; }

    /// <summary>
    /// Json arguments
    /// </summary>
    public JsonDocument? Arguments { get; set; }

    /// <summary>
    /// Agent where the code script is stored
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// States
    /// </summary>
    public List<MessageState>? States { get; set; } = null;
}
