namespace BotSharp.Abstraction.Users.Enums;

public static class UserConstant
{
    public static IEnumerable<string> AdminRoles = new List<string>
    {
        UserRole.Admin,
        UserRole.Root
    };
}
