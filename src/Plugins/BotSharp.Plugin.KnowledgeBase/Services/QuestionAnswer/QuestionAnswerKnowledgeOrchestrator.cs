namespace BotSharp.Plugin.KnowledgeBase.Services;

public class QuestionAnswerKnowledgeOrchestrator : VectorOrchestratorBase, IKnowledgeOrchestrator
{
    public override string Type => KnowledgeType.QuestionAnswer;

    public QuestionAnswerKnowledgeOrchestrator(
        IServiceProvider services,
        ILogger<QuestionAnswerKnowledgeOrchestrator> logger,
        KnowledgeBaseSettings settings)
        : base(services, logger, settings)
    {
    }
}