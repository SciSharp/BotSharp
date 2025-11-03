
namespace BotSharp.Abstraction.FuzzSharp
{
    public interface IVocabularyService
    {
        Task<Dictionary<string, HashSet<string>>> LoadVocabularyAsync(string? folderPath);
        Task<Dictionary<string, (string DbPath, string CanonicalForm)>> LoadDomainTermMappingAsync(string? filePath);
    }
}