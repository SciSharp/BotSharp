using BotSharp.Abstraction.VectorStorage.Extensions;

namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class KnowledgeRetrievalFn : IFunctionCallback
{
    public string Name => "knowledge_retrieval";

    public string Indication => "searching my brain";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public KnowledgeRetrievalFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        var collectionName = _settings.Default.CollectionName ?? KnowledgeCollectionName.BotSharp;
        var knowledgeService = _services.GetRequiredService<IKnowledgeService>();
        var knowledges = await knowledgeService.SearchVectorKnowledge(args.Question, collectionName, new VectorSearchOptions
        {
            Fields = new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer },
            Confidence = 0.2f
        });

        if (!knowledges.IsNullOrEmpty())
        {
            var answers = knowledges.Select(x => x.ToQuestionAnswer()).ToList();
            message.Content = string.Join("\r\n\r\n=====\r\n", answers);
        }
        else
        {
            message.Content = $"I didn't find any useful knowledge related to [{args.Question}]. \r\nCan you tell me the instruction and I'll memorize it.";
            message.StopCompletion = true;
        }

        return true;
    }
}
