namespace BotSharp.Plugin.MongoStorage.Collections;

public class TranslationMemoryDocument : MongoBase
{
    public string OriginalText { get; set; } = default!;
    public string HashText { get; set; } = default!;
    public List<TranslationMemoryMongoElement> Translations { get; set; } = [];
}
