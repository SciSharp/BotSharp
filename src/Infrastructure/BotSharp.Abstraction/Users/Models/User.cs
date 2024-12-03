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
    public string Source { get; set; } = UserSource.Internal;
    public string? ExternalId { get; set; }
    /// <summary>
    /// internal, client, affiliate
    /// </summary>
    public string Type { get; set; } = UserType.Client;
    public string Role { get; set; } = UserRole.User;
    public string? VerificationCode { get; set; }
    public bool Verified { get; set; }
    public string RegionCode { get; set; } = "CN";
    public string? AffiliateId { get; set; }
    public string? ReferralCode { get; set; }
    public string? EmployeeId { get; set; }
    public bool IsDisabled { get; set; }
    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<UserAgentAction> AgentActions { get; set; } = [];
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
