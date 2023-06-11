namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    [StringLength(36)]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    /// <summary>
    /// Owner user id
    /// </summary>
    public string OwerId { get; set; } = string.Empty;
}
