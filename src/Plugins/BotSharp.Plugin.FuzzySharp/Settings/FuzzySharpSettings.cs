namespace BotSharp.Plugin.FuzzySharp.Settings;

public class FuzzySharpSettings
{
    public TokenDataSettings Data { get; set; }
    public MembaseNERSettings Membase { get; set; } = new();
}

public class TokenDataSettings
{
    public string? BaseDir { get; set; } = "data/tokens";

    public TokenFileSetting Vocabulary { get; set; }
    public TokenFileSetting Synonym { get; set; }
}

public class TokenFileSetting
{
    public string Folder { get; set; }
    public string[] FileNames { get; set; }
}

public class MembaseNERSettings
{
    // Per-tenant vocabulary sources. Outer key = graphId; inner key = graph node Label
    // (used in "MATCH (n:Label) ..."); value = projection list for that label.
    public Dictionary<string, Dictionary<string, VocabularyFieldSetting[]>> VocabularySources { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
}

public class VocabularyFieldSetting
{
    // Property name on the graph node, used to build "RETURN n.GraphProperty".
    public string GraphProperty { get; set; } = string.Empty;

    // "table.column" string surfaced to downstream consumers
    // (becomes FlaggedTokenItem.Sources / EntityAnalysisResult.Data["sources"]).
    public string SqlSource { get; set; } = string.Empty;
}