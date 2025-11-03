
namespace BotSharp.Plugin.FuzzySharp.Repository
{
    public interface IVocabularyRepository
    {
        Task<Dictionary<string, HashSet<string>>> FetchTableColumnValuesAsync();
    }
}