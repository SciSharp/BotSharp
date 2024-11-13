namespace BotSharp.Abstraction.Repositories.Filters;

public class UserFilter : Pagination
{
    [JsonPropertyName("user_ids")]
    public IEnumerable<string>? UserIds { get; set; }

    [JsonPropertyName("user_names")]
    public IEnumerable<string>? UserNames { get; set; }

    [JsonPropertyName("external_ids")]
    public IEnumerable<string>? ExternalIds { get; set; }

    [JsonPropertyName("roles")]
    public IEnumerable<string>? Roles { get; set; }

    [JsonPropertyName("sources")]
    public IEnumerable<string>? Sources { get; set; }

    public static UserFilter Empty()
    {
        return new UserFilter();
    }
}
