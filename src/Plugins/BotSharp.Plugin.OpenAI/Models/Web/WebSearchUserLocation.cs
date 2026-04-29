namespace BotSharp.Plugin.OpenAI.Models.Web;

/// <summary>
/// Approximate user location hint passed to the OpenAI web search tool.
/// Mirrors the shape of OpenAI's user_location payload (country, region, city, timezone).
/// </summary>
public class WebSearchUserLocation
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Timezone { get; set; }

    [JsonIgnore]
    public bool HasAnyValue =>
        !string.IsNullOrEmpty(Country)
        || !string.IsNullOrEmpty(Region)
        || !string.IsNullOrEmpty(City)
        || !string.IsNullOrEmpty(Timezone);

    public static WebSearchUserLocation? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WebSearchUserLocation>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

}
