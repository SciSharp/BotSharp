namespace BotSharp.Abstraction.Files.Models;

public class LlmFileContext
{
    [JsonPropertyName("user_request")]
    public string UserRequest { get; set; }

    [JsonPropertyName("file_types")]
    public string FileTypes { get; set; }
}
