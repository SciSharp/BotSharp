namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class MemorizeKnowledgeFn : IFunctionCallback
{
    public string Name => "memorize_knowledge";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public MemorizeKnowledgeFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        var embedding = _services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == _settings.TextEmbedding.Provider);
        embedding.SetModelName(_settings.TextEmbedding.Model);

        var vector = await embedding.GetVectorsAsync(new List<string>
        {
            args.Question
        });

        var vectorDb = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Name == _settings.VectorDb);
        var collectionName = !string.IsNullOrWhiteSpace(_settings.DefaultCollection) ? _settings.DefaultCollection : KnowledgeCollectionName.BotSharp;
        await vectorDb.CreateCollection(collectionName, vector[0].Length);

        var id = Guid.NewGuid().ToString();
        var result = await vectorDb.Upsert(collectionName, id, vector[0], 
            args.Question, 
            new Dictionary<string, string> 
            { 
                { KnowledgePayloadName.Answer, args.Answer } 
            });

        message.Content = result ? "Saved to my brain" : "I forgot it";
        return true;
    }
}
