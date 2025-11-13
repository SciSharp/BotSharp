
namespace BotSharp.Plugin.FuzzySharp.FuzzSharp.Models;

public class TextAnalysisResponse
{
    public string Original { get; set; } = string.Empty;
    public List<string>? Tokens { get; set; }
    public List<FlaggedItem> Flagged { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
}