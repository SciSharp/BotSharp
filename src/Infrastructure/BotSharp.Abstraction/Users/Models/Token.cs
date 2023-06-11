namespace BotSharp.Abstraction.Users.Models;

public class Token
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpireTime { get; set; }
    public string Scope { get; set; } = string.Empty;
}
