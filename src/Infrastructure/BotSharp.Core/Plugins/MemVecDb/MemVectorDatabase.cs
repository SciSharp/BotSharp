using BotSharp.Abstraction.VectorStorage;
using System.IO;
using Tensorflow.NumPy;

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

    public Task<List<string>> Search(string collectionName, float[] vector, int limit = 10)
    {
        var similarities = new float[_vectors[collectionName].Count];
        for (int i = 0; i < _vectors[collectionName].Count; i++)
        {
            similarities[i] = CalCosineSimilarity(vector, _vectors[collectionName][i].Vector);
        }

        var texts = np.argsort(similarities).ToArray<int>()
            .Reverse()
            .Take(limit)
            .Select(i => _vectors[collectionName][i].Text)
            .ToList();

        return Task.FromResult(texts);
    }

    public Task Upsert(string collectionName, int id, float[] vector, string text)
    {
        _vectors[collectionName].Add(new VecRecord
        {
            Id = id,
            Vector = vector,
            Text = text
        });

        return Task.CompletedTask;
    }

    private float CalCosineSimilarity(float[] a, float[] b)
    {
        return np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b));
    }
}
