namespace BotSharp.Plugin.FuzzySharp.Settings;

public class FuzzySharpSettings
{
    public TokenDataSettings Data { get; set; }
}

public class TokenDataSettings
{
    public string? BaseDir { get; set; }
    public string? VocabularyFolder { get; set; }
    public string? SynonymFileName { get; set; }
}