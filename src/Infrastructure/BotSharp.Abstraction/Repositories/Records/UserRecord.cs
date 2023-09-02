using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Repositories.Records;

public class UserRecord : RecordBase
{
    [Required]
    [MaxLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Salt { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(36)]
    public string ExternalId { get; set; }

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
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Salt = Salt,
            Password = Password,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
