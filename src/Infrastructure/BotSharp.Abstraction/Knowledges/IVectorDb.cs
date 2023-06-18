namespace BotSharp.Abstraction.Knowledges;

public interface IVectorDb
{
    Task<List<string>> GetCollections();
    Task CreateCollection(string collectionName);
    Task Upsert(string collectionName, int id, float[] vector);
    Task<List<int>> Search(string collectionName, float[] vector, int limit = 10);
}
