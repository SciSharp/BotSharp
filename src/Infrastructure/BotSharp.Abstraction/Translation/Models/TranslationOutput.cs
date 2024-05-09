namespace BotSharp.Abstraction.Translation.Models;

public class TranslationOutput
{
    [JsonPropertyName("input_lang")]
    public string InputLanguage { get; set; } = null!;

    [JsonPropertyName("output_lang")]
    public string OutputLanguage { get; set; } = LanguageType.ENGLISH;

    [JsonPropertyName("texts")]
    public string[] Texts { get; set; } = Array.Empty<string>();
}
