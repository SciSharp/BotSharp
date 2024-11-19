using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Abstraction.Repositories.Filters;

public class RoleFilter
{
    [JsonPropertyName("names")]
    public IEnumerable<string>? Names { get; set; }

    [JsonPropertyName("exclude_roles")]
    public IEnumerable<string>? ExcludeRoles { get; set; } = UserConstant.AdminRoles;


    public static RoleFilter Empty()
    {
        return new RoleFilter();
    }
}
