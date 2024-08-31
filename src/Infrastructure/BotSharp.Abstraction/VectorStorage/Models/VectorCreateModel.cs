namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCreateModel
{
    public string Text { get; set; }
    public Dictionary<string, string>? Payload { get; set; }
}
