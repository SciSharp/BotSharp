using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Abstraction.Users.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Salt { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Source { get; set; } = "internal";
    public string? ExternalId { get; set; }
    public string Role { get; set; } = UserRole.Client;
    public string? VerificationCode { get; set; }
    public bool Verified { get; set; }
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
