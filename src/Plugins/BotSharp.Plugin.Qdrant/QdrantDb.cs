using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.DependencyInjection;
using QdrantCSharp;
using QdrantCSharp.Enums;
using QdrantCSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Qdrant;

public class QdrantDb : IVectorDb
{
    private readonly QdrantHttpClient _client;
    private readonly QdrantSetting _setting;
    private readonly IServiceProvider _services;

    public QdrantDb(QdrantSetting setting,
        IServiceProvider services)
    {
        _setting = setting;
        _services = services;
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

    public async Task CreateCollection(string collectionName, int dim)
    {
        var collections = await GetCollections();
        if (!collections.Contains(collectionName))
        {
            // Create a new collection
            await _client.CreateCollection(collectionName, new VectorParams(size: dim, distance: Distance.COSINE));

            var agentService = _services.GetRequiredService<IAgentService>();
            var agentDataDir = agentService.GetAgentDataDir(collectionName);
            var knowledgePath = Path.Combine(agentDataDir, "knowledge.txt");
            File.WriteAllLines(knowledgePath, new string[0]);
        }

        // Get collection info
        var collectionInfo = await _client.GetCollection(collectionName);
        if (collectionInfo == null)
        {
            throw new Exception($"Create {collectionName} failed.");
        }
    }

    public async Task Upsert(string collectionName, int id, float[] vector, string text)
    {
        // Insert vectors
        /*await _client.Upsert(collectionName, points: new List<PointStruct>
        {
            new PointStruct(id: id, vector: vector)
        });*/

        // Store chunks in local file system
        var agentService = _services.GetRequiredService<IAgentService>();
        var agentDataDir = agentService.GetAgentDataDir(collectionName);
        var knowledgePath = Path.Combine(agentDataDir, "knowledge.txt");
        File.AppendAllLines(knowledgePath, new[] { text });
    }

    public async Task<List<string>> Search(string collectionName, float[] vector, int limit = 5)
    {
        var result = await _client.Search(collectionName, vector, limit);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agentDataDir = agentService.GetAgentDataDir(collectionName);
        var knowledgePath = Path.Combine(agentDataDir, "knowledge.txt");
        var texts = File.ReadAllLines(knowledgePath);

        return result.Result.Select(x => texts[x.Id]).ToList();
    }
}
