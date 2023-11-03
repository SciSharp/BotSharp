using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Instructs.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class MessageResponseModel : InstructResult
{
    public string Function { get; set; }

    /// <summary>
    /// Planner instruction
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCallFromLlm Instruction { get; set; }

    /// <summary>
    /// Rich message for UI rendering
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? RichContent { get; set; }
}
