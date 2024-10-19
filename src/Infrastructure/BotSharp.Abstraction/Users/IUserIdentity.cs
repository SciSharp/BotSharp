namespace BotSharp.Abstraction.Users;

public interface IUserIdentity
{
    string Id { get; }
    string Email { get; }
    string UserName { get; }
    string FirstName { get; }
    string LastName { get; }
    string FullName { get; }
    string? UserLanguage { get; }
    string? Phone { get; }
    string? AffiliateId { get; }
    string? EmployeeId { get; }
    string Type { get; }
    string Role { get; }
}
