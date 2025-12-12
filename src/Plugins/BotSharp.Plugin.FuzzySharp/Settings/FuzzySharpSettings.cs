namespace BotSharp.Plugin.FuzzySharp.Settings;

public class FuzzySharpSettings
{
    public TokenDataSettings Data { get; set; }
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