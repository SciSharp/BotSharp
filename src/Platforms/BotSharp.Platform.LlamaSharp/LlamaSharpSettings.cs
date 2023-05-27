namespace BotSharp.Platform.LlamaSharp;

public class LlamaSharpSettings
{
    public string ModelPath { get; set; }
    public string InstructionFile { get; set; }
    public int MaxContextLength { get; set; } = 512;
}
