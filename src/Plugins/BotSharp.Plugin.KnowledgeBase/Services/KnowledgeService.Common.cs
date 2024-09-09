namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<bool> RefreshVectorKnowledgeConfigs(VectorCollectionConfigsModel configs)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var collections = configs.Collections ?? new();
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);

        foreach (var collection in collections)
        {
            collection.CreateDate = DateTime.UtcNow;
            collection.CreateUserId = user.Id;
        }

        var saved = db.ResetKnowledgeCollectionConfigs(collections);
        return await Task.FromResult(saved);
    }
}
