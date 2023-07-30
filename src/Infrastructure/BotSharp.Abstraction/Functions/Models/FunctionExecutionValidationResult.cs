using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionExecutionValidationResult
{
    public FunctionExecutionValidationResult()
    {

    }

    public FunctionExecutionValidationResult(string validationStatus, string? validationMessage = null)
    {
        ValidationStatus = validationStatus;
        ValidationMessage = validationMessage;
    }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; set; }

    [JsonPropertyName("validation_message")]
    public string ValidationMessage { get; set; }
}
