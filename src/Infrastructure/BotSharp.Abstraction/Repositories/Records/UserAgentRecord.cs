using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Repositories.Records;

public class UserAgentRecord : RecordBase
{
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
