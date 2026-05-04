namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class DocumentKnowledgeBase : VectorKnowledgeBase, IKnowledgeService
{
    public override string KnowledgeType => KnowledgeBaseType.Document;

    public DocumentKnowledgeBase(
        IServiceProvider services,
        ILogger<DocumentKnowledgeBase> logger,
        KnowledgeBaseSettings settings)
        : base(services, logger, settings)
    {
    }
}
