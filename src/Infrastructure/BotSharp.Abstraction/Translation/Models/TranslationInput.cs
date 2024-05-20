namespace BotSharp.Abstraction.Translation.Models;

public class TranslationInput
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = -1;

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}
