namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ITextChopper _textChopper;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(IServiceProvider services,
        KnowledgeBaseSettings settings,
        ITextChopper textChopper,
        ILogger<KnowledgeService> logger)
    {
        _services = services;
        _settings = settings;
        _textChopper = textChopper;
        _logger = logger;
    }

    public async Task EmbedKnowledge(KnowledgeCreationModel knowledge)
    {
        var idStart = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 32,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding();

        await db.CreateCollection(KnowledgeCollectionName.BotSharp, textEmbedding.Dimension);
        foreach (var line in lines)
        {
            var vec = await textEmbedding.GetVectorAsync(line);
            await db.Upsert(KnowledgeCollectionName.BotSharp, idStart.ToString(), vec, line);
            idStart++;
            Console.WriteLine($"Saved vector {idStart}/{lines.Count}: {line}\n");
        }
    }

    public async Task Feed(KnowledgeFeedModel knowledge)
    {
        var idStart = 0;
        var lines = _textChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 32,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding();

        await db.CreateCollection(knowledge.AgentId, textEmbedding.Dimension);
        foreach (var line in lines)
        {
            var vec = await textEmbedding.GetVectorAsync(line);
            await db.Upsert(knowledge.AgentId, idStart.ToString(), vec, line);
            idStart++;
            Console.WriteLine($"Saved vector {idStart}/{lines.Count}: {line}\n");
        }
    }

    public async Task<string> GetKnowledges(KnowledgeRetrievalModel retrievalModel)
    {
        var textEmbedding = GetTextEmbedding();
        var vector = await textEmbedding.GetVectorAsync(retrievalModel.Question);

        // Vector search
        var db = GetVectorDb();
        var result = await db.Search(KnowledgeCollectionName.BotSharp, vector, KnowledgePayloadName.Answer, limit: 10);

        // Restore 
        return string.Join("\n\n", result.Select((x, i) => $"### Paragraph {i + 1} ###\n{x.Trim()}"));
    }

    public async Task<List<RetrievedResult>> GetAnswer(KnowledgeRetrievalModel retrievalModel)
    {
        // Restore 
        var prompt = await GetKnowledges(retrievalModel);

        var sb = new StringBuilder(prompt);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("------");
        sb.AppendLine("Answer question based on the given information above. Keep your answers concise. Please response with paragraph number, cite sources and reasoning in JSON format, if multiple paragraphs are found, put them in a JSON array. make sure the paragraph number is real. If you don't know the answer just output empty.");
        sb.AppendLine("[" + JsonSerializer.Serialize(new RetrievedResult()) + "]");
        sb.AppendLine("------");
        sb.AppendLine($"QUESTION: \"{retrievalModel.Question}\"");
        sb.AppendLine("Which paragraphs are relevant in order to answer the above question?");
        sb.AppendLine("ANSWER: ");
        prompt = sb.ToString().Trim();

        var completion = await GetTextCompletion().GetCompletion(prompt, Guid.Empty.ToString(), Guid.Empty.ToString());
        return JsonSerializer.Deserialize<List<RetrievedResult>>(completion);
    }

    public IVectorDb GetVectorDb()
    {
        var db = _services.GetServices<IVectorDb>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.VectorDb));
        return db;
    }

    public ITextEmbedding GetTextEmbedding()
    {
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextEmbedding));
        return embedding;
    }

    public ITextCompletion GetTextCompletion()
    {
        var textCompletion = _services.GetServices<ITextCompletion>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextCompletion));
        return textCompletion;
    }
}
