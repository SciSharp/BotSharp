namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> RefreshVectorKnowledgeConfigs(VectorCollectionConfigsModel configs)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var collections = configs.Collections ?? new();
        var userId = await GetUserId();

        foreach (var collection in collections)
        {
            collection.CreateDate = DateTime.UtcNow;
            collection.CreateUserId = userId;
        }

        var saved = db.AddKnowledgeCollectionConfigs(collections, reset: true);
        return await Task.FromResult(saved);
    }
}
