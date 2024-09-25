namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> RefreshVectorKnowledgeConfigs(VectorCollectionConfigsModel configs)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var collections = configs.Collections ?? new();
        var saved = db.AddKnowledgeCollectionConfigs(collections, reset: true);
        return await Task.FromResult(saved);
    }
}
