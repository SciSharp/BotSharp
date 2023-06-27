using BotSharp.Abstraction.VectorStorage;
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

    public Task<List<int>> Search(string collectionName, float[] vector, int limit = 10)
    {
        var cosineList = new List<double>();
        for (int i = 0; i < _vectors[collectionName].Count; i++)
        {
            var p = CalCosineSimilarity(vector, _vectors[collectionName][i].Vector);
            cosineList.Add(p);
        }
        var similarities = cosineList.ToArray();
        var indice = np.argsort(similarities).ToArray<int>()
            .Reverse().Take(limit).ToList();
        return Task.FromResult(indice);
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

    private double CalCosineSimilarity(float[] vector1, float[] vector2)
    {
        NDArray a = vector1;
        NDArray b = vector2;
        double num = np.dot(a, b);
        if(num == 0)
        {
            return 0.0;
        }

        b = np.square(a);
        var x = np.sqrt(np.sum(b));
        var x3 = np.sum(np.square(vector2));
        double num2 = np.sqrt(x) * np.sqrt(x3);
        if(num2 == 0)
        {
            return 0.0;
        }
        return num / num2;
    }
}
