namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(
        IServiceProvider services,
        KnowledgeBaseSettings settings,
        ILogger<KnowledgeService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    private IVectorDb GetVectorDb()
    {
        var db = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Provider == _settings.VectorDb.Provider);
        return db;
    }

    private IGraphDb GetGraphDb()
    {
        var db = _services.GetServices<IGraphDb>().FirstOrDefault(x => x.Provider == _settings.GraphDb.Provider);
        return db;
    }

    private ITextEmbedding GetTextEmbedding(string collection)
    {
        return KnowledgeSettingHelper.GetTextEmbeddingSetting(_services, collection);
    }
}
