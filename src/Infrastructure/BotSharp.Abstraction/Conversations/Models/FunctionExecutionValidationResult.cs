using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Conversations.Models;

public class FunctionExecutionValidationResult : IFunctionExecutionResult
{
    private string _validationStatus;
    public string _validationMessage;

    public FunctionExecutionValidationResult(string validationStatus, string validationMessage = "")
    {
        _validationStatus = validationStatus;
        _validationMessage = validationMessage;
    }

    [JsonPropertyName("validation_status")]
    public string ValidationStatus => _validationStatus;

    [JsonPropertyName("validation_message")]
    public string ValidationMessage => _validationMessage;
}
