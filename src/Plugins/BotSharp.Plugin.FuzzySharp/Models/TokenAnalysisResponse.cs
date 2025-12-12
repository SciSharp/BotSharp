namespace BotSharp.Plugin.FuzzySharp.Models;

public class TokenAnalysisResponse
{
    public string Original { get; set; } = string.Empty;
    public List<string>? Tokens { get; set; }
    public List<FlaggedTokenItem> Flagged { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
}