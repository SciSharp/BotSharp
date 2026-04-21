namespace BotSharp.Plugin.KnowledgeBase.Services;

public abstract partial class VectorOrchestratorBase : IKnowledgeOrchestrator
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected readonly KnowledgeBaseSettings _settings;

    public abstract string KnowledgeType { get; }

    protected VectorOrchestratorBase(
        IServiceProvider services,
        ILogger logger,
        KnowledgeBaseSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    protected IVectorDb? GetVectorDb(string? dbProvider = null)
    {
        var provider = dbProvider.IfNullOrEmptyAs(_settings.VectorDb.Provider);
        var db = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Provider == provider);
        return db;
    }

    protected async Task<ITextEmbedding> GetTextEmbedding(string collectionName)
    {
        return await KnowledgeSettingHelper.GetTextEmbeddingSetting(_services, collectionName);
    }

    protected async Task<string> GetUserId()
    {
        var userIdentity = _services.GetRequiredService<IUserIdentity>();
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(userIdentity.Id);
        return user.Id;
    }
}
