using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    string Provider { get; }

    Task<bool> DoesCollectionExist(string collectionName);
    Task<IEnumerable<string>> GetCollections();
    Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter);
    Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids, bool withPayload = false, bool withVector = false);
    Task<bool> CreateCollection(string collectionName, int dimension);
    Task<bool> DeleteCollection(string collectionName);
    Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, string>? payload = null);
    Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector, IEnumerable<string>? fields, int limit = 5, float confidence = 0.5f, bool withVector = false);
    Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids);
    Task<bool> DeleteCollectionAllData(string collectionName);
}
