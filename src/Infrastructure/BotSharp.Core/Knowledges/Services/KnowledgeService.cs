using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Knowledges.Models;
using System.IO;
using System.Collections;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Knowledges.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly ITextEmbedding _textEmbedding;
    private readonly ITextCompletion _textCompletion;
    private readonly IVectorDb _db;
    string collectionName = "my_collection";
    public KnowledgeService(ITextEmbedding textEmbedding, 
        ITextCompletion textCompletion, 
        IVectorDb db)
    {
        _textEmbedding = textEmbedding;
        _textCompletion = textCompletion;
        _db = db;
    }

    public async Task Feed(KnowledgeFeedModel knowledge)
    {
        var idStart = 0;
        var lines = knowledge.Content.Split(". ");
        lines = lines.Select((x, i) => $"{i+1} {x}").ToArray();
        File.WriteAllLines(collectionName + ".txt", lines);

        foreach (var line in lines)
        {
            idStart++;
            await _db.Upsert(collectionName, idStart, _textEmbedding.GetVector(line));
        }
    }

    public async Task<string> GetAnswer(string question)
    {
        var vector = _textEmbedding.GetVector(question);

        // Vector search
        var result = await _db.Search(collectionName, vector);

        var prompt = "";
        var lines = File.ReadAllLines(collectionName + ".txt");
        foreach (var r in result)
        {
            prompt += lines[r - 1] + "\n";
        }

        prompt += "###\r\n";
        prompt += "Answer the user's question based on the content provided above, and your reply should be as concise and organized as possible.\r\n";
        prompt += "Q: how to turn on Hood Light? \r\nA: Press the Hood Light keypad to turn the light beneath the hood on or off.\r\n";
        prompt += $"Q: {question}\r\nA: ";

        var completion = await _textCompletion.GetCompletion(prompt);
        return completion;
    }
}
