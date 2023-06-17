namespace BotSharp.Abstraction.Users;

public interface IUserIdentity
{
    string Id { get; }
    string Email { get; }
    string FirstName { get; }
    string LastName { get; }
}
