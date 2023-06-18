namespace BotSharp.Core.Knowledges;

public class KnowledgeBase : IVectorDb
{
    public Task CreateCollection(string collectionName, int dim)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetCollections()
    {
        throw new NotImplementedException();
    }

    public Task<List<int>> Search(string collectionName, float[] vector, int limit = 10)
    {
        throw new NotImplementedException();
    }

    public Task Upsert(string collectionName, int id, float[] vector)
    {
        throw new NotImplementedException();
    }
}
