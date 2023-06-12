using BotSharp.Abstraction.Conversations.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Core.Repository.DbTables;

[Table("Session")]
public class SessionRecord : DbRecord, IAgentTable
{
    [Required]
    [MaxLength(36)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static SessionRecord FromSession(Session sess)
    {
        return new SessionRecord
        {
            AgentId = sess.AgentId,
            UserId = sess.UserId,
            Id = sess.Id,
            Title = sess.Title,
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }

    public Session ToSession()
    {
        return new Session
        {
            Id = Id,
            Title = Title,
            UserId = UserId,
            AgentId = AgentId,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
