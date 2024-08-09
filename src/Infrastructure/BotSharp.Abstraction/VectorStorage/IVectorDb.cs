namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    string Name { get; }

    Task<IEnumerable<string>> GetCollections();
    Task<StringIdPagedItems<KnowledgeCollectionData>> GetCollectionData(string collectionName, KnowledgeFilter filter);
    Task CreateCollection(string collectionName, int dim);
    Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null);
    Task<IEnumerable<KnowledgeSearchResult>> Search(string collectionName, float[] vector, IEnumerable<string> fields, int limit = 5, float confidence = 0.5f, bool withVector = false);
    Task<bool> DeleteCollectionData(string collectionName, string id);
}
