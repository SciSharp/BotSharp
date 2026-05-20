namespace BotSharp.Plugin.KnowledgeBase.Services;

public class QuestionAnswerKnowledgeBase : VectorKnowledgeBase, IKnowledgeService
{
    public override string KnowledgeType => KnowledgeBaseType.QuestionAnswer;

    public QuestionAnswerKnowledgeBase(
        IServiceProvider services,
        ILogger<QuestionAnswerKnowledgeBase> logger,
        KnowledgeBaseSettings settings)
        : base(services, logger, settings)
    {
    }
}