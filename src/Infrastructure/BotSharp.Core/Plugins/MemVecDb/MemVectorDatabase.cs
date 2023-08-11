using BotSharp.Abstraction.VectorStorage;
using Tensorflow;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

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

    public NDArray CalCosineSimilarity(float[] vec, List<VecRecord> records)
    {
        var recordsArray = np.zeros((records.Count, records[0].Vector.Length), dtype: np.float32);

        for (int i = 0; i < records.Count; i++)
        {
            recordsArray[i] = records[i].Vector;
        }

        var vecArray = np.expand_dims(np.array(vec, dtype: np.float32), axis: 0); // [1. 300]

        (var normVecArray, var _) = SafeNormalize(vecArray);
        (var normRecordsArray, var _) = SafeNormalize(recordsArray);

        var simiMatix = tf.matmul(tf.cast(normVecArray, tf.float32), tf.transpose(tf.cast(normRecordsArray, tf.float32))).numpy(); // [1, num_records]

        simiMatix = np.squeeze(simiMatix, axis: 0);

        return simiMatix;
    }

    public int[] CalCosineSimilarityTopK(float[] vec, List<VecRecord> records, int topK = 10, float filterProb = 0.75f)
    {
        var simiMatix = CalCosineSimilarity(vec, records);

        var topIndex = np.argsort(simiMatix)["::-1"][$":{topK}"];

        var resIndex = new List<int>();

        for (int i = 0; i < topK; i++)
        {
            var index = topIndex[i];
            var value = simiMatix[index];

            if (value > filterProb)
            {
                resIndex.Add(topIndex[i]);
            }
        }

        return resIndex.ToArray();
    }

    public (NDArray, NDArray) SafeNormalize(NDArray x, double eps = 2.223E-15)
    {
        var squaredX = np.sum(np.multiply(x, x), axis: 1);
        var normX = np.sqrt(squaredX);

        var epsTensor = tf.cast(tf.convert_to_tensor(eps), dtype: tf.float32);
        var normXTensor = tf.cast(normX, tf.float32);
        var contantMask = (normXTensor < epsTensor);
        var divideTensor = tf.ones_like(normXTensor, dtype: tf.float32);

        normX = tf.where(contantMask, divideTensor, normXTensor).numpy();
        normX = np.expand_dims(normX, axis: 1);

        return (x / normX, normX);
    }
}
