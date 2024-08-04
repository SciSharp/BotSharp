namespace BotSharp.Abstraction.Files.Models;

public class InputMessageFiles
{
    public List<BotSharpFile> Files { get; set; } = new List<BotSharpFile>();
    public BotSharpFile? Mask { get; set; }
}
