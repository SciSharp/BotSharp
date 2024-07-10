namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    Task<List<string>> GetCollections();
    Task CreateCollection(string collectionName, int dim);
    Task Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null);
    Task<List<string>> Search(string collectionName, float[] vector, int limit = 5);
}
