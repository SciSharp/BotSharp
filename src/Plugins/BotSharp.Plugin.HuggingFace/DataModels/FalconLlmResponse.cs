namespace BotSharp.Plugin.HuggingFace.DataModels;

public class FalconLlmResponse
{
    [JsonPropertyName("generated_text")]
    public string GeneratedText {  get; set; }
}
