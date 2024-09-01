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

        var collectionName = _settings.Default.CollectionName ?? KnowledgeCollectionName.BotSharp;
        var embedding = KnowledgeSettingHelper.GetTextEmbeddingSetting(_services, collectionName);

        var vector = await embedding.GetVectorsAsync(new List<string>
        {
            args.Question
        });

        var vectorDb = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Provider == _settings.VectorDb.Provider);
        await vectorDb.CreateCollection(collectionName, vector[0].Length);

        var result = await vectorDb.Upsert(collectionName, Guid.NewGuid(), vector[0], 
            args.Question,
            new Dictionary<string, string> 
            { 
                { KnowledgePayloadName.Answer, args.Answer } 
            });

        message.Content = result ? "Saved to my brain" : "I forgot it";
        return true;
    }
}
