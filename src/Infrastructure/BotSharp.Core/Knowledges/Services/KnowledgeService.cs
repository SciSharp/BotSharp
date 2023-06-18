using BotSharp.Abstraction.Knowledges.Models;
using System.IO;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Knowledges.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly ITextEmbedding _textEmbedding;
    private readonly ITextCompletion _textCompletion;
    private readonly ITextChopper _textChopper;
    private readonly IVectorDb _db;
    
    public KnowledgeService(ITextEmbedding textEmbedding, 
        ITextCompletion textCompletion, 
        ITextChopper textChopper,
        IVectorDb db)
    {
        _textEmbedding = textEmbedding;
        _textCompletion = textCompletion;
        _textChopper = textChopper;
        _db = db;
    }

    public async Task Feed(KnowledgeFeedModel knowledge)
    {
        var idStart = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 256,
            Conjunction = 32
        });

        // Store chunks in local file system
        var knowledgeStoreDir = Path.Combine("knowledge_chunks", knowledge.AgentId);
        if(!Directory.Exists(knowledgeStoreDir))
        {
            Directory.CreateDirectory(knowledgeStoreDir);
        }

        var knowledgePath = Path.Combine(knowledgeStoreDir, knowledge.Name);
        File.WriteAllLines(knowledgePath + ".txt", lines);

        await _db.CreateCollection(knowledge.Name, _textEmbedding.Dimension);
        foreach (var line in lines)
        {
            await _db.Upsert(knowledge.Name, idStart, _textEmbedding.GetVector(line));
            idStart++;
        }
    }

    public async Task<string> GetAnswer(KnowledgeRetrievalModel retrievalModel)
    {
        var vector = _textEmbedding.GetVector(retrievalModel.Question);

        // Scan local knowledge directory
        var knowledgeName = "";
        var chunks = new string[0];

        foreach (var file in Directory.GetFiles(Path.Combine("knowledge_chunks", retrievalModel.AgentId)))
        {
            knowledgeName = new FileInfo(file).Name.Split('.').First();
            chunks = File.ReadAllLines(file);
        }

        // Vector search
        var result = await _db.Search(knowledgeName, vector);

        // Restore 
        var prompt = "";
        foreach (var r in result)
        {
            prompt += chunks[r] + "\n";
        }

        prompt += "\r\n###\r\n";
        prompt += "Answer the user's question based on the content provided above, and your reply should be as concise and organized as possible.\r\n";
        prompt += $"Question: {retrievalModel.Question}\r\nAnswer: ";

        var completion = await _textCompletion.GetCompletion(prompt);
        return completion;
    }
}
