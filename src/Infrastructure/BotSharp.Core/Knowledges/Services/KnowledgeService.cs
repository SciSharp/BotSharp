using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Knowledges.Models;
using QdrantCSharp.Enums;
using QdrantCSharp.Models;
using QdrantCSharp;
using System.IO;
using System.Collections;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Knowledges.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly ITextEmbedding _textEmbedding;
    private readonly ITextCompletion _textCompletion;
    string collectionName = "my_collection";
    public KnowledgeService(ITextEmbedding textEmbedding, ITextCompletion textCompletion)
    {
        _textEmbedding = textEmbedding;
        _textCompletion = textCompletion;
    }

    public QdrantHttpClient GetClient()
    {
        var client = new QdrantHttpClient
        (
            url: "",
            apiKey: ""
        );
        return client;
    }

    public async Task Feed(KnowledgeFeedModel knowledge)
    {
        var client = GetClient();

        // List all the collections
        var collections = await client.GetCollections();
        if (!collections.Result.Collections.Select(x => x.Name).Contains(collectionName))
        {
            // Create a new collection
            await client.CreateCollection(collectionName, new VectorParams(size: 300, distance: Distance.COSINE));
        }

        // Get collection info
        var collectionInfo = await client.GetCollection(collectionName);
        var idStart = 0;
        var lines = knowledge.Content.Split(". ");
        lines = lines.Select((x, i) => $"{i+1} {x}").ToArray();
        File.WriteAllLines(collectionName + ".txt", lines);

        foreach (var line in lines)
        {
            idStart++;
            
            // Insert vectors
            /*await client.Upsert(collectionName, points: new List<PointStruct>
            {
                new PointStruct(id: idStart, vector: _textEmbedding.GetVector(line))
            });*/
        }
    }

    public async Task<string> GetAnswer(string question)
    {
        var client = GetClient();
        var vector = _textEmbedding.GetVector(question);

        // Vector search
        var result = await client.Search
        (
            collectionName,
            vector,
            limit: 10
        );

        var prompt = "";
        var lines = File.ReadAllLines(collectionName + ".txt");
        foreach (var r in result.Result)
        {
            prompt += lines[r.Id - 1] + "\n";
        }

        prompt += "###\r\n";
        prompt += "Answer the user's question based on the content provided above, and your reply should be as concise and organized as possible.\r\n";
        prompt += "Q: how to turn on Hood Light? \r\nA: Press the Hood Light keypad to turn the light beneath the hood on or off.\r\n";
        prompt += $"Q: {question}\r\nA: ";

        var completion = await _textCompletion.GetCompletion(prompt);
        return completion;
    }
}
