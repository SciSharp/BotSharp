namespace BotSharp.Plugin.LLamaSharp.Settings;

public class LlamaSharpSettings
{
    public string ModelDir { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "llama-2-7b-chat.Q8_0.gguf";
    public int MaxContextLength { get; set; } = 512;
    public float RepeatPenalty { get; set; } = 1.0f;
    public bool VerbosePrompt { get; set; }
    public bool Interactive { get; set; } = true;
    public int NumberOfGpuLayer { get; set; }
}
