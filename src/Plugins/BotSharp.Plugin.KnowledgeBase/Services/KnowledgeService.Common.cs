namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> RefreshVectorKnowledgeConfigs(VectorCollectionConfigsModel configs)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var saved = db.SaveKnowledgeCollectionConfigs(configs.Collections);
        return await Task.FromResult(saved);
    }
}
