using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Conversations.Models;

public class FunctionExecutionValidationResult
{
    public FunctionExecutionValidationResult(string validationStatus, string validationMessage = "")
    {
        ValidationStatus = validationStatus;
        ValidationMessage = validationMessage;
    }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; set; }

    [JsonPropertyName("validation_message")]
    public string ValidationMessage { get; set; }
}
