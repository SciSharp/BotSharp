using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.VectorStorage;

namespace BotSharp.Core.Plugins.Knowledges.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ITextChopper _textChopper;

    public KnowledgeService(IServiceProvider services,
        KnowledgeBaseSettings settings,
        ITextChopper textChopper)
    {
        _services = services;
        _settings = settings;
        _textChopper = textChopper;
    }

    public async Task Feed(KnowledgeFeedModel knowledge)
    {
        var idStart = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 256,
            Conjunction = 5,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding();

        await db.CreateCollection(knowledge.AgentId, textEmbedding.Dimension);
        foreach (var line in lines)
        {
            var vec = textEmbedding.GetVector(line);
            await db.Upsert(knowledge.AgentId, idStart, vec, line);
            idStart++;
        }
    }

    public async Task<string> GetKnowledges(KnowledgeRetrievalModel retrievalModel)
    {
        var textEmbedding = GetTextEmbedding();
        var vector = textEmbedding.GetVector(retrievalModel.Question);

        // Vector search
        var result = await GetVectorDb().Search(retrievalModel.AgentId, vector, limit: 10);

        // Restore 
        return "### Helpful domain knowledges:\r\n" + string.Join("\n", result.Select((x, i) => $"{i + 1}: {x}"));
    }

    public async Task<string> GetAnswer(KnowledgeRetrievalModel retrievalModel)
    {
        // Restore 
        var prompt = await GetKnowledges(retrievalModel);

        prompt += "\r\n### Answer user's question by utilizing the helpful domain knowledges above.\r\n";
        prompt += $"\r\nQuestion: {retrievalModel.Question}\r\nAnswer: ";

        var completion = await GetTextCompletion().GetCompletion(prompt);
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

    public ITextCompletion GetTextCompletion()
    {
        var textCompletion = _services.GetServices<ITextCompletion>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextCompletion));
        return textCompletion;
    }
}
