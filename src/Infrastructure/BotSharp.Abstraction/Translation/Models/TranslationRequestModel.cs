using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.OpenAPI.ViewModels.Translations;

public class TranslationRequestModel
{
    public string Text { get; set; } = null!;
    public string ToLang { get; set; } = LanguageType.CHINESE;
}
