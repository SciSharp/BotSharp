namespace BotSharp.Plugin.FuzzySharp.Models;

public class TextAnalysisRequest
{
    public string Text { get; set; } = string.Empty;
    public string? VocabularyFolderName { get; set; }
    public string? SynonymMappingFile { get; set; }
    public double Cutoff { get; set; } = 0.82;
    public int TopK { get; set; } = 5;
    public int MaxNgram { get; set; } = 5;
    public bool IncludeTokens { get; set; } = false;
}