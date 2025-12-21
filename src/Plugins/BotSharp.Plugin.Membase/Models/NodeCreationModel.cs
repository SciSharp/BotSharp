namespace BotSharp.Plugin.Membase.Models;

public class NodeCreationModel
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public object? Properties { get; set; }
    public DateTime? Time { get; set; }

    public Node ToNode()
    {
        return new Node
        {
            Id = Id,
            Labels = Labels?.ToList() ?? new List<string>(),
            Properties = Properties ?? new(),
            Time = Time ?? DateTime.UtcNow
        };
    }
}
