using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Translations;

public class TranslationResponseModel
{
    public string Text { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FromLang { get; set; } = null!;
}
