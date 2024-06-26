using Microsoft.Extensions.Logging;
using System.Threading;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public IEnumerable<TranslationMemoryOutput> GetTranslationMemories(IEnumerable<TranslationMemoryQuery> queries)
    {
        var list = new List<TranslationMemoryOutput>();
        if (queries.IsNullOrEmpty())
        {
            return list;
        }

        var hashTexts = queries.Where(x => !string.IsNullOrEmpty(x.HashText)).Select(x => x.HashText).ToList();
        var filter = Builders<TranslationMemoryDocument>.Filter.In(x => x.HashText, hashTexts);
        var memories = _dc.TranslationMemories.Find(filter).ToList();
        if (memories.IsNullOrEmpty()) return list;

        foreach (var query in queries)
        {
            if (string.IsNullOrWhiteSpace(query.HashText) || string.IsNullOrWhiteSpace(query.Language))
            {
                continue;
            }

            var foundMemory = memories.FirstOrDefault(x => x.HashText.Equals(query.HashText));
            if (foundMemory == null) continue;

            var foundItem = foundMemory.Translations?.FirstOrDefault(x => x.Language.Equals(query.Language));
            if (foundItem == null) continue;

            list.Add(new TranslationMemoryOutput
            {
                OriginalText = query.OriginalText,
                TranslatedText = foundItem.TranslatedText,
                HashText = foundMemory.HashText,
                Language = foundItem.Language,
            });
        }

        return list;
    }

    public bool SaveTranslationMemories(IEnumerable<TranslationMemoryInput> inputs)
    {
        if (inputs.IsNullOrEmpty()) return false;

        var hashTexts = inputs.Where(x => !string.IsNullOrEmpty(x.HashText)).Select(x => x.HashText).ToList();
        var filter = Builders<TranslationMemoryDocument>.Filter.In(x => x.HashText, hashTexts);
        var memories = _dc.TranslationMemories.Find(filter)?.ToList() ?? new List<TranslationMemoryDocument>();

        var newMemories = new List<TranslationMemoryDocument>();
        var updateMemories = new List<TranslationMemoryDocument>();

        try
        {
            foreach (var input in inputs)
            {
                if (string.IsNullOrWhiteSpace(input.OriginalText) ||
                    string.IsNullOrWhiteSpace(input.TranslatedText) ||
                    string.IsNullOrWhiteSpace(input.HashText) ||
                    string.IsNullOrWhiteSpace(input.Language))
                {
                    continue;
                }

                var newItem = new TranslationMemoryMongoElement
                {
                    TranslatedText = input.TranslatedText,
                    Language = input.Language
                };

                var foundMemory = memories?.FirstOrDefault(x => x.HashText.Equals(input.HashText));
                if (foundMemory == null)
                {
                    newMemories.Add(new TranslationMemoryDocument
                    {
                        Id = Guid.NewGuid().ToString(),
                        OriginalText = input.OriginalText,
                        HashText = input.HashText,
                        Translations = new List<TranslationMemoryMongoElement> { newItem }
                    });
                }
                else
                {
                    var foundItem = foundMemory.Translations?.FirstOrDefault(x => x.Language.Equals(input.Language));
                    if (foundItem != null) continue;

                    if (foundMemory.Translations == null)
                    {
                        foundMemory.Translations = new List<TranslationMemoryMongoElement> { newItem };
                    }
                    else
                    {
                        foundMemory.Translations.Add(newItem);
                    }
                    updateMemories.Add(foundMemory);
                }
            }

            if (!newMemories.IsNullOrEmpty())
            {
                _dc.TranslationMemories.InsertMany(newMemories);
            }

            if (!updateMemories.IsNullOrEmpty())
            {
                foreach (var mem in updateMemories)
                {
                    var updateFilter = Builders<TranslationMemoryDocument>.Filter.Eq(x => x.Id, mem.Id);
                    _dc.TranslationMemories.ReplaceOne(updateFilter, mem);
                    Thread.Sleep(50);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving translation memories: {ex.Message}");
            return false;
        }
    }
}
