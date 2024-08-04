namespace BotSharp.Abstraction.Translation.Models;

public class TranslationMemory
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("original_text")]
    public string OriginalText { get; set; }

    [JsonPropertyName("hash_text")]
    public string HashText { get; set; }

    [JsonPropertyName("translations")]
    public List<TranslationMemoryItem> Translations { get; set; } = new List<TranslationMemoryItem>();
}

public class TranslationMemoryItem
{
    [JsonPropertyName("translated_text")]
    public string TranslatedText { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }
}