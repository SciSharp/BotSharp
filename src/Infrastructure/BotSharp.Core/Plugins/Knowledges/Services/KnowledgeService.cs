using BotSharp.Abstraction.Knowledges.Models;
using System.IO;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.VectorStorage;

namespace BotSharp.Core.Plugins.Knowledges.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ITextCompletion _textCompletion;
    private readonly ITextChopper _textChopper;

    public KnowledgeService(IServiceProvider services,
        KnowledgeBaseSettings settings,
        ITextCompletion textCompletion,
        ITextChopper textChopper)
    {
        _services = services;
        _settings = settings;
        _textCompletion = textCompletion;
        _textChopper = textChopper;
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
        if (!Directory.Exists(knowledgeStoreDir))
        {
            Directory.CreateDirectory(knowledgeStoreDir);
        }

        var knowledgePath = Path.Combine(knowledgeStoreDir, "chuncks");
        File.WriteAllLines(knowledgePath + ".txt", lines);

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding();

        await db.CreateCollection(knowledge.AgentId, textEmbedding.Dimension);
        foreach (var line in lines)
        {
            var vec = textEmbedding.GetVector(line);
            await db.Upsert(knowledge.AgentId, idStart, vec);
            idStart++;
        }
    }

    public async Task<string> GetAnswer(KnowledgeRetrievalModel retrievalModel)
    {
        var textEmbedding = GetTextEmbedding();
        var vector = textEmbedding.GetVector(retrievalModel.Question);

        // Scan local knowledge directory
        var knowledgeName = "";
        var chunks = new string[0];

        foreach (var file in Directory.GetFiles(Path.Combine("knowledge_chunks", retrievalModel.AgentId)))
        {
            knowledgeName = new FileInfo(file).Name.Split('.').First();
            chunks = File.ReadAllLines(file);
        }

        // Vector search
        var result = await GetVectorDb().Search(knowledgeName, vector);

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

    public IVectorDb GetVectorDb()
    {
        var db = _services.GetServices<IVectorDb>()
            .FirstOrDefault(x => x.GetType().Name == _settings.VectorDb);
        return db;
    }

    public ITextEmbedding GetTextEmbedding()
    {
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().Name == _settings.TextEmbedding);
        return embedding;
    }
}
