namespace BotSharp.Abstraction.Knowledges;

public interface IPhraseCollection
{
    Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync();
    Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadDomainTermMappingAsync();
}
