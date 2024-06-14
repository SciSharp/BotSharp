namespace BotSharp.Plugin.MongoStorage.Models;

public class TranslationMemoryMongoElement
{
    public string TranslatedText { get; set; }
    public string Language { get; set; }

    public static TranslationMemoryMongoElement ToMongoElement(TranslationMemoryItem item)
    {
        return new TranslationMemoryMongoElement
        {
            TranslatedText = item.TranslatedText,
            Language = item.Language
        };
    }

    public static TranslationMemoryItem ToDomainElement(TranslationMemoryMongoElement element)
    {
        return new TranslationMemoryItem
        {
            TranslatedText = element.TranslatedText,
            Language = element.Language
        };
    }
}
