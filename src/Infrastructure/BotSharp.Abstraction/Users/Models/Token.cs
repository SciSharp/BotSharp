using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Users.Models;

public class Token
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
    [JsonPropertyName("expires")]
    public int ExpireTime { get; set; }
    public string Scope { get; set; } = string.Empty;
}
