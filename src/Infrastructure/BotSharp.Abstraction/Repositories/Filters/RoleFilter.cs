using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Abstraction.Repositories.Filters;

public class RoleFilter
{
    public IEnumerable<string>? Names { get; set; }
    public IEnumerable<string>? ExcludeRoles { get; set; } = UserConstant.AdminRoles;


    public static RoleFilter Empty()
    {
        return new RoleFilter();
    }

    public bool IsInit()
    {
        return Names.IsNullOrEmpty();
    }
}
