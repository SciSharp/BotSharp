using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace BotSharp.Plugin.Qdrant;

public class QdrantDb : IVectorDb
{
    private QdrantClient _client;
    private readonly QdrantSetting _setting;
    private readonly IServiceProvider _services;

    public QdrantDb(QdrantSetting setting,
        IServiceProvider services)
    {
        _setting = setting;
        _services = services;

    }

    private QdrantClient GetClient()
    {
        if (_client == null)
        {
            _client = new QdrantClient
            (
                host: _setting.Url,
                https: true,
                apiKey: _setting.ApiKey
            );
        }
        return _client;
    }

    public async Task<List<string>> GetCollections()
    {
        // List all the collections
        var collections = await GetClient().ListCollectionsAsync();
        return collections.ToList();
    }

    public async Task CreateCollection(string collectionName, int dim)
    {
        var collections = await GetCollections();
        if (!collections.Contains(collectionName))
        {
            // Create a new collection
            await GetClient().CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = (ulong)dim,
                Distance = Distance.Cosine
            });
        }

        // Get collection info
        var collectionInfo = await _client.GetCollectionInfoAsync(collectionName);
        if (collectionInfo == null)
        {
            throw new Exception($"Create {collectionName} failed.");
        }
    }

    public async Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null)
    {
        // Insert vectors
        var point = new PointStruct()
        {
            Id = new PointId()
            {
                Uuid = id
            },
            Vectors = vector,

            Payload = 
            {
                { KnowledgePayloadName.Text, text }
            }
        };

        if (payload != null)
        {
            foreach (var item in payload)
            {
                point.Payload.Add(item.Key, item.Value);
            }
        }

        var client = GetClient();

        var result = await client.UpsertAsync(collectionName, points: new List<PointStruct>
        {
            point
        });

        return result.Status == UpdateStatus.Completed;
    }

    public async Task<List<string>> Search(string collectionName, float[] vector, string returnFieldName, int limit = 5, float confidence = 0.5f)
    {
        var client = GetClient();
        var points = await client.SearchAsync(collectionName, vector, 
            limit: (ulong)limit,
            scoreThreshold: confidence);

        return points.Select(x => x.Payload[returnFieldName].StringValue).ToList();
    }
}
