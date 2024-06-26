namespace BotSharp.Plugin.MongoStorage.Collections;

public class TranslationMemoryDocument : MongoBase
{
    public string OriginalText { get; set; }
    public string HashText { get; set; }
    public List<TranslationMemoryMongoElement> Translations { get; set; } = new List<TranslationMemoryMongoElement>();
}
