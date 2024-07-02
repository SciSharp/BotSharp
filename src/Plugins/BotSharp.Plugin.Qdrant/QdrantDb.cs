using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null)
    {
        // Insert vectors
        var point = new PointStruct()
        {
            Id = new PointId()
            {
                Uuid = id
            },
            Vectors = vector,
            Payload = { }
        };

        foreach (var item in payload)
        {
            point.Payload.Add(item.Key, item.Value);
        }

        var result = await GetClient().UpsertAsync(collectionName, points: new List<PointStruct>
        {
            point
        });
    }

    public async Task<List<string>> Search(string collectionName, float[] vector, int limit = 5)
    {
        var result = await GetClient().SearchAsync(collectionName, vector, limit: (ulong)limit);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agentDataDir = agentService.GetAgentDataDir(collectionName);
        var knowledgePath = Path.Combine(agentDataDir, "knowledge.txt");
        var texts = File.ReadAllLines(knowledgePath);

        return result.Select(x => texts[x.Id.Num]).ToList();
    }
}
