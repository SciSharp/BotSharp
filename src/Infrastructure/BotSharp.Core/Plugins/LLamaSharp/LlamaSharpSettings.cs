namespace BotSharp.Core.Plugins.LLamaSharp;

public class LlamaSharpSettings
{
    public string ModelPath { get; set; } = string.Empty;
    public string InstructionFile { get; set; } = string.Empty;
    public string ChatSampleFile { get; set; } = string.Empty;
    public int MaxContextLength { get; set; } = 512;
    public float RepeatPenalty { get; set; } = 1.0f;
    public bool VerbosePrompt { get; set; }
    public bool Interactive { get; set; } = true;
    public int NumberOfGpuLayer { get; set; }
}
