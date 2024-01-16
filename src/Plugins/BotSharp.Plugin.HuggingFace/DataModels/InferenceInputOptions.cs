namespace BotSharp.Plugin.HuggingFace.DataModels;

public class InferenceInputOptions
{
    [JsonPropertyName("use_cache")]
    public bool UseCache { get; set; } = true;

    [JsonPropertyName("wait_for_model")]
    public bool WaitForModel { get; set; } = true;
}
