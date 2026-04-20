namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class DocumentKnowledgeOrchestrator : VectorOrchestratorBase, IKnowledgeOrchestrator
{
    public override string Type => KnowledgeType.Document;

    public DocumentKnowledgeOrchestrator(
        IServiceProvider services,
        ILogger<DocumentKnowledgeOrchestrator> logger,
        KnowledgeBaseSettings settings)
        : base(services, logger, settings)
    {
    }
}
