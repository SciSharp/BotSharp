using BotSharp.Abstraction.Entity.Models;

namespace BotSharp.Abstraction.Entity;

public interface IEntityDataLoader
{
    string Provider { get; }

    /// <summary>
    /// Load vocabulary data => return mapping: [data source] = a list of vocabularies
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync();

    /// <summary>
    /// Load synonym data => return mapping: [word/phrase] = (data source, canonical form)
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync();

    /// <summary>
    /// Context-aware vocabulary load. Default implementation delegates to the
    /// parameterless version for loaders that don't need runtime parameters.
    /// </summary>
    Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync(EntityDataLoadContext ctx)
        => LoadVocabularyAsync();

    /// <summary>
    /// Context-aware synonym load. Default implementation delegates to the
    /// parameterless version for loaders that don't need runtime parameters.
    /// </summary>
    Task<Dictionary<string, (string DataSource, string CanonicalForm)>> LoadSynonymMappingAsync(EntityDataLoadContext ctx)
        => LoadSynonymMappingAsync();
}
