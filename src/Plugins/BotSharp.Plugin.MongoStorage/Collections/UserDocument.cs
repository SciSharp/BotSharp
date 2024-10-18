using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class UserDocument : MongoBase
{
    public string UserName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Salt { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Source { get; set; } = UserSource.Internal;
    public string? ExternalId { get; set; }
    public string Type { get; set; } = UserType.Client;
    public string Role { get; set; } = null!;
    public string? VerificationCode { get; set; }
    public bool Verified { get; set; }
    public string? RegionCode { get; set; }
    public string? AffiliateId { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public User ToUser()
    {
        return new User
        {
            Id = Id,
            UserName = UserName,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Phone = Phone,
            Password = Password,
            Salt = Salt,
            Source = Source,
            ExternalId = ExternalId,
            Type = Type,
            Role = Role,
            AffiliateId = AffiliateId,
            IsDisabled = IsDisabled,
            VerificationCode = VerificationCode,
            Verified = Verified,
        };
    }
}