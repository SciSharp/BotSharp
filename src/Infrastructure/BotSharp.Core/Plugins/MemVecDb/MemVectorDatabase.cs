using BotSharp.Abstraction.VectorStorage;

namespace BotSharp.Core.Plugins.MemVecDb;

public class MemVectorDatabase : IVectorDb
{
    private readonly Dictionary<string, int> _collections = new Dictionary<string, int>();
    private readonly Dictionary<string, List<VecRecord>> _vectors = new Dictionary<string, List<VecRecord>>();
    public Task CreateCollection(string collectionName, int dim)
    {
        _collections[collectionName] = dim;
        _vectors[collectionName] = new List<VecRecord>();
        return Task.CompletedTask;
    }

    public Task<List<string>> GetCollections()
    {
        return Task.FromResult(_collections.Select(x => x.Key).ToList());
    }

    public Task<List<int>> Search(string collectionName, float[] vector, int limit = 10)
    {
        throw new NotImplementedException();
    }

    public Task Upsert(string collectionName, int id, float[] vector)
    {
        _vectors[collectionName].Add(new VecRecord
        {
            Id = id,
            Vector = vector
        });

        return Task.CompletedTask;
    }
}
