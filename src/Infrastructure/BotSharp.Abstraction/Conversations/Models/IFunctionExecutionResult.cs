using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Conversations.Models;

public class IFunctionExecutionResult
{
    [JsonPropertyName("execution_status")]
    public FunctionExecutionStatus ExecutionStatus { get; set; }
}
