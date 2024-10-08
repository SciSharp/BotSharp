using Tensorflow.NumPy;

namespace BotSharp.Plugin.KnowledgeBase.MemVecDb;

public class MemoryVectorDb : IVectorDb
{
    private readonly Dictionary<string, int> _collections = new Dictionary<string, int>();
    private readonly Dictionary<string, List<VecRecord>> _vectors = new Dictionary<string, List<VecRecord>>();


    public string Provider => "MemoryVector";


    public async Task<bool> DoesCollectionExist(string collectionName)
    {
        return false;
    }

    public async Task<bool> CreateCollection(string collectionName, int dimension)
    {
        _collections[collectionName] = dimension;
        _vectors[collectionName] = new List<VecRecord>();
        return true;
    }

    public async Task<bool> DeleteCollection(string collectionName)
    {
        return false;
    }

    public async Task<IEnumerable<string>> GetCollections()
    {
        return _collections.Select(x => x.Key).ToList();
    }

    public Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids,
        bool withPayload = false, bool withVector = false)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector,
        IEnumerable<string>? fields, int limit = 5, float confidence = 0.5f, bool withVector = false)
    {
        if (!_vectors.ContainsKey(collectionName))
        {
            return new List<VectorCollectionData>();
        }

        var similarities = VectorHelper.CalCosineSimilarity(vector, _vectors[collectionName]);

        var results = np.argsort(similarities).ToArray<int>()
                        .Reverse()
                        .Take(limit)
                        .Select(i => new VectorCollectionData
                        {
                            Data = new Dictionary<string, object> { { "text", _vectors[collectionName][i].Text } },
                            Score = similarities[i],
                            Vector = withVector ? _vectors[collectionName][i].Vector : null,
                        })
                        .ToList();

        return await Task.FromResult(results);
    }

    public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, object>? payload = null)
    {
        _vectors[collectionName].Add(new VecRecord
        {
            Id = id.ToString(),
            Vector = vector,
            Text = text
        });

        return true;
    }

    public async Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> DeleteCollectionAllData(string collectionName)
    {
        return await Task.FromResult(false);
    }
}
