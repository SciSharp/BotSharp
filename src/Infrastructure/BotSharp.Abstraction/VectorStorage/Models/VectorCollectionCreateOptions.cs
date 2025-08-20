namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionCreateOptions
{
    public int Dimension { get; set; }
    public string Provider { get; set; } = null!;
    public string Model { get; set; } = null!;
}
