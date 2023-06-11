namespace BotSharp.Abstraction.Users;

public interface ICurrentUser
{
    string Id { get; }
    string Email { get; }
    string FirstName { get; }
    string LastName { get; }
}
