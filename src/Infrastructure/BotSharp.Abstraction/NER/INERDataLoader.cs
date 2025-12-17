namespace BotSharp.Abstraction.NER;

public interface INERDataLoader
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
}
