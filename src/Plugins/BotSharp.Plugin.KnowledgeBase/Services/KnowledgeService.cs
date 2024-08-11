namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ITextChopper _textChopper;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(
        IServiceProvider services,
        KnowledgeBaseSettings settings,
        ITextChopper textChopper,
        ILogger<KnowledgeService> logger)
    {
        _services = services;
        _settings = settings;
        _textChopper = textChopper;
        _logger = logger;
    }

    private IVectorDb GetVectorDb()
    {
        var db = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Name == _settings.VectorDb);
        return db;
    }

    private ITextEmbedding GetTextEmbedding()
    {
        var embedding = _services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == _settings.TextEmbedding.Provider);
        if (embedding != null)
        {
            embedding.SetModelName(_settings.TextEmbedding.Model);
        }
        return embedding;
    }
}
