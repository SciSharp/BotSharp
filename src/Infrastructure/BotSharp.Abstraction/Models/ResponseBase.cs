namespace BotSharp.Abstraction.Models;

public class ResponseBase
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMsg { get; set; }
}
