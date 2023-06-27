namespace BotSharp.Abstraction.VectorStorage;

public interface IVectorDb
{
    Task<List<string>> GetCollections();
    Task CreateCollection(string collectionName, int dim);
    Task Upsert(string collectionName, int id, float[] vector, string text);
    Task<List<string>> Search(string collectionName, float[] vector, int limit = 10);
}
