using BotSharp.Abstraction.Translation.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public IEnumerable<TranslationMemoryOutput> GetTranslationMemories(IEnumerable<TranslationMemoryQuery> queries)
    {
        var list = new List<TranslationMemoryOutput>();
        if (queries.IsNullOrEmpty())
        {
            return list;
        }

        var dir = Path.Combine(_dbSettings.FileRepository, "translation");
        var file = Path.Combine(dir, TRANSLATION_MEMORY_FILE);
        if (!Directory.Exists(dir) || !File.Exists(file))
        {
            return list;
        }

        var content = File.ReadAllText(file);
        if (string.IsNullOrWhiteSpace(content))
        {
            return list;
        }

        var memories = ReadTranslationMemoryContent(content);
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

        try
        {
            var dir = Path.Combine(_dbSettings.FileRepository, "translation");
            var file = Path.Combine(dir, TRANSLATION_MEMORY_FILE);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var content = string.Empty;
            if (File.Exists(file))
            {
                content = File.ReadAllText(file);
            }
            
            var memories = ReadTranslationMemoryContent(content);

            foreach (var input in inputs)
            {
                if (string.IsNullOrWhiteSpace(input.OriginalText) ||
                    string.IsNullOrWhiteSpace(input.TranslatedText) ||
                    string.IsNullOrWhiteSpace(input.HashText) ||
                    string.IsNullOrWhiteSpace(input.Language))
                {
                    continue;
                }

                var newItem = new TranslationMemoryItem
                {
                    TranslatedText = input.TranslatedText,
                    Language = input.Language
                };

                var foundMemory = memories?.FirstOrDefault(x => x.HashText.Equals(input.HashText));
                if (foundMemory == null)
                {
                    var newMemory = new TranslationMemory
                    {
                        Id = Guid.NewGuid().ToString(),
                        OriginalText = input.OriginalText,
                        HashText = input.HashText,
                        Translations = new List<TranslationMemoryItem> { newItem }
                    };

                    if (memories == null)
                    {
                        memories = new List<TranslationMemory> { newMemory };
                    }
                    else
                    {
                        memories.Add(newMemory);
                    }
                }
                else
                {
                    var foundItem = foundMemory.Translations?.FirstOrDefault(x => x.Language.Equals(input.Language));
                    if (foundItem != null) continue;

                    if (foundMemory.Translations == null)
                    {
                        foundMemory.Translations = new List<TranslationMemoryItem> { newItem };
                    }
                    else
                    {
                        foundMemory.Translations.Add(newItem);
                    }
                }
            }

            var json = JsonSerializer.Serialize(memories, _options);
            File.WriteAllText(file, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving translation memories: {ex.Message}");
            return false;
        }
    }

    private List<TranslationMemory> ReadTranslationMemoryContent(string? content)
    {
        var memories = new List<TranslationMemory>();

        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return memories;
            }

           memories = JsonSerializer.Deserialize<List<TranslationMemory>>(content, _options) ?? new List<TranslationMemory>();
        }
        catch {}

        return memories;
    }
}
