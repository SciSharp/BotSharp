
namespace BotSharp.Plugin.FuzzySharp.FuzzSharp.Models;

public class FlaggedItem
{
    public int Index { get; set; }
    public string Token { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
    public string MatchType { get; set; } = string.Empty;
    public string CanonicalForm { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int NgramLength { get; set; }
}
