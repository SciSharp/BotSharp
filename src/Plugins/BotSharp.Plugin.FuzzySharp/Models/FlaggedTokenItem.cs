namespace BotSharp.Plugin.FuzzySharp.Models;

public class FlaggedTokenItem
{
    public int Index { get; set; }
    public string Token { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
    public MatchPriority MatchType { get; set; } = new();
    public string CanonicalForm { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int NgramLength { get; set; }
}
