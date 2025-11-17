namespace BotSharp.Abstraction.Knowledges.Responses;

public class UploadKnowledgeResponse
{
    [JsonPropertyName("success")]
    public IEnumerable<string> Success { get; set; } = new List<string>();

    [JsonPropertyName("failed")]
    public IEnumerable<string> Failed { get; set; } = new List<string>();

    [JsonPropertyName("is_success")]
    public bool IsSuccess {
        get
        {
            return !Success.IsNullOrEmpty() && Failed.IsNullOrEmpty();
        }
    }
}
