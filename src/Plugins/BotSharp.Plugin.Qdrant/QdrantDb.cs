using BotSharp.Abstraction.Utilities;
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

    public async Task<StringIdPagedItems<KnowledgeCollectionData>> GetCollectionData(string collectionName, KnowledgeFilter filter)
    {
        var client = GetClient();
        
        var exists = await client.CollectionExistsAsync(collectionName);
        if (!exists)
        {
            return new StringIdPagedItems<KnowledgeCollectionData>();
        }

        var totalPointCount = await client.CountAsync(collectionName);
        var response = await client.ScrollAsync(collectionName, limit: (uint)filter.Size, 
            offset: !string.IsNullOrWhiteSpace(filter.StartId) ? new PointId { Uuid = filter.StartId } : 0,
            vectorsSelector: filter.WithVector);
        var points = response?.Result?.Select(x => new KnowledgeCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Question = x.Payload.ContainsKey(KnowledgePayloadName.Text) ? x.Payload[KnowledgePayloadName.Text].StringValue : string.Empty,
            Answer = x.Payload.ContainsKey(KnowledgePayloadName.Answer) ? x.Payload[KnowledgePayloadName.Answer].StringValue : string.Empty,
            Vector = filter.WithVector ? x.Vectors?.Vector?.Data?.ToArray() : null
        })?.ToList() ?? new List<KnowledgeCollectionData>();

        return new StringIdPagedItems<KnowledgeCollectionData>
        {
            Count = totalPointCount,
            NextId = response?.NextPageOffset?.Uuid,
            Items = points
        };
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
