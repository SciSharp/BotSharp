using BotSharp.Abstraction.Knowledges;
using QdrantCSharp;
using QdrantCSharp.Enums;
using QdrantCSharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Qdrant;

public class QdrantDb : IVectorDb
{
    private readonly QdrantHttpClient _client;
    private readonly QdrantSetting _setting;
    public QdrantDb(QdrantSetting setting)
    {
        _setting = setting;
        _client = new QdrantHttpClient
        (
            url: _setting.Url,
            apiKey: _setting.ApiKey
        );
    }

    public async Task<List<string>> GetCollections()
    {
        // List all the collections
        var collections = await _client.GetCollections();
        return collections.Result.Collections.Select(x => x.Name).ToList();
    }

    public async Task CreateCollection(string collectionName)
    {
        var collections = await GetCollections();
        if (!collections.Contains(collectionName))
        {
            // Create a new collection
            await _client.CreateCollection(collectionName, new VectorParams(size: 300, distance: Distance.COSINE));
        }

        // Get collection info
        var collectionInfo = await _client.GetCollection(collectionName);
        if(collectionInfo == null)
        {
            throw new Exception($"Create {collectionName} failed.");
        }
    }

    public async Task Upsert(string collectionName, int id, float[] vector)
    {
        // Insert vectors
        await _client.Upsert(collectionName, points: new List<PointStruct>
        {
            new PointStruct(id: id, vector: vector)
        });
    }

    public async Task<List<int>> Search(string collectionName, float[] vector, int limit = 10)
    {
        var result = await _client.Search(collectionName, vector, limit);
        return result.Result.Select(x => x.Id).ToList();
    }
}
