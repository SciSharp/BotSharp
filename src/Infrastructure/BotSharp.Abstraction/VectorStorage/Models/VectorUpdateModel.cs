namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorUpdateModel
{
    public string Id { get; set; }
    public string Text { get; set; }
    public Dictionary<string, string>? Payload { get; set; }
}
