using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    Task<List<string>> GetCollections();
    Task<KnowledgeCollectionInfo> GetCollectionInfo(string collectionName);
    Task<StringIdPagedItems<KnowledgeCollectionData>> GetCollectionData(string collectionName, KnowledgeFilter filter);
    Task CreateCollection(string collectionName, int dim);
    Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null);
    Task<List<string>> Search(string collectionName, float[] vector, string returnFieldName, int limit = 5, float confidence = 0.5f);
}
