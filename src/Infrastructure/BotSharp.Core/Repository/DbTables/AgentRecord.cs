using BotSharp.Abstraction.Agents.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Core.Repository.DbTables;

[Table("Agent")]
public class AgentRecord : DbRecord, IAgentTable
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(36)]
    public string OwnerId { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDateTime { get; set; }

    [Required]
    public DateTime UpdatedDateTime { get; set; }

    public static AgentRecord FromAgent(Agent agent)
    {
        return new AgentRecord
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            OwnerId = agent.OwerId
        };
    }

    public Agent ToAgent()
    {
        return new Agent
        {
            Id = Id,
            Name = Name,
            Description = Description,
            OwerId = OwnerId,
            CreatedDateTime = CreatedDateTime,
            UpdatedDateTime = UpdatedDateTime
        };
    }
}
