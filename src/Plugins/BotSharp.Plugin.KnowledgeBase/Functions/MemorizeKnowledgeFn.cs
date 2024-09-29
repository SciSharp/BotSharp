namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class MemorizeKnowledgeFn : IFunctionCallback
{
    public string Name => "memorize_knowledge";

    public string Indication => "remembering knowledge";

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
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var result = await knowledgeService.CreateVectorCollectionData(collectionName, new VectorCreateModel
        {
            Text = args.Question,
            Payload = new Dictionary<string, object>
            {
                { KnowledgePayloadName.Answer, args.Answer }
            }
        });

        message.Content = result ? "Saved to my brain" : "I forgot it";
        return true;
    }
}
