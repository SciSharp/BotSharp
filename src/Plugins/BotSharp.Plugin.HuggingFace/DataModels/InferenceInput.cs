namespace BotSharp.Plugin.HuggingFace.DataModels;

public class InferenceInput
{
    [JsonPropertyName("inputs")]
    public string Inputs { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public InferenceInputParameters Parameters { get; set; } 
        = new InferenceInputParameters();

    [JsonPropertyName("options")]
    public InferenceInputOptions Options { get; set; } 
        = new InferenceInputOptions();
}
