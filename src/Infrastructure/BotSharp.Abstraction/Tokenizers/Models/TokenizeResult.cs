namespace BotSharp.Abstraction.Tokenizers.Models;

public class TokenizeResult
{
    public string Token { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = [];
    public string CanonicalForm { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
