namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionSnapshot
{
    public string Name { get; set; } = default!;
    public long Size { get; set; }
    public DateTime CreatedTime { get; set; }
    public string? CheckSum { get; set; }
}