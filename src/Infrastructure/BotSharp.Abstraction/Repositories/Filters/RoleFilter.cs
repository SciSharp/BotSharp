namespace BotSharp.Abstraction.Repositories.Filters;

public class RoleFilter
{
    [JsonPropertyName("names")]
    public IEnumerable<string>? Names { get; set; }

    public static RoleFilter Empty()
    {
        return new RoleFilter();
    }
}
