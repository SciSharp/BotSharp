namespace BotSharp.OpenAPI.ViewModels.Translations;

public class TranslationRequestModel
{
    public string Text { get; set; } = null!;
    public string ToLang { get; set; } = LanguageType.CHINESE;
}

public class TranslationScriptTimestamp
{
    public string Text {  set; get; } = null!;
    public string Timestamp { get; set; } = null!;
}

public class TranslationLongTextRequestModel
{
    public TranslationScriptTimestamp[] Texts { get; set; } = null!;
    public string ToLang { get; set; } = LanguageType.CHINESE;
}