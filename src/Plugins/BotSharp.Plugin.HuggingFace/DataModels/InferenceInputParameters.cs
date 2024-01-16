namespace BotSharp.Plugin.HuggingFace.DataModels;

public class InferenceInputParameters
{
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.7f;

    [JsonPropertyName("max_new_tokens")]
    public int MaxNewTokens { get; set; } = 128;

    [JsonPropertyName("return_full_text")]
    public bool ReturnFullText { get; set; } = false;

    [JsonPropertyName("num_return_sequences")]
    public int NumReturnSequences { get; set; } = 1;
}
