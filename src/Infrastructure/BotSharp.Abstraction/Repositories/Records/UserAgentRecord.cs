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

    public static UserRecord FromUser(User user)
    {
        return new UserRecord
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password
        };
    }

    public User ToUser()
    {
        return new User
        {
            Id = Id,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
