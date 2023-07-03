using BotSharp.Abstraction.VectorStorage;
using System.Collections;
using System.IO;
using System.Numerics;
using Tensorflow;
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

    public Task<List<string>> Search(string collectionName, float[] vector, int limit = 5)
    {
        var similarities = CalCosineSimilarity(vector, _vectors[collectionName]);
        // var similarities2 = CalEuclideanDistance(vector, _vectors[collectionName]);

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

    private float[] CalEuclideanDistance(float[] vec, List<VecRecord> records)
    {
        var a = np.zeros((records.Count, vec.Length), np.float32);
        var b = np.zeros((records.Count, vec.Length), np.float32);
        for (var i = 0; i < records.Count; i++)
        {
            a[i] = vec;
            b[i] = records[i].Vector;
        }

        var c = np.sqrt(np.sum(np.square(a - b), axis: 1));
        // var c = -np.prod(np.linalg.norm(a, axis: 1) * np.linalg.norm(b, axis: 1), axis: 1);
        return c.ToArray<float>();
    }

    private float[] CalCosineSimilarity(float[] vec, List<VecRecord> records)
    {
        var similarities = new float[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            var a = vec;
            var b = records[i].Vector;
            similarities[i] = np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b));
        }
        return similarities;
    }
}
