namespace BotSharp.Plugin.HuggingFace.DataModels;

public class TextGenResponse
{
    [JsonPropertyName("generated_text")]
    public string GeneratedText {  get; set; }
}
