namespace BotSharp.Abstraction.Models;

public class AiModel
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public int TokenLimit { get; set; }
}
