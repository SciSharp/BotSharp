using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class RenewTokenModel
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}
