namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeBaseAgentHook : AgentHookBase, IAgentHook
{
    private const string KNOWLEDGE_RETRIEVAL_FN = "knowledge_retrieval";

    public override string SelfId => string.Empty;

    public KnowledgeBaseAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {

    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtility
        {
            Name = UtilityName.KnowledgeRetrieval,
            Content = new UtilityContent
            {
                Functions = [new(KNOWLEDGE_RETRIEVAL_FN)],
                Templates = [new($"{KNOWLEDGE_RETRIEVAL_FN}.fn")]
            }
        };

        base.OnLoadAgentUtility(agent, [utilityLoad]);
        base.OnAgentLoaded(agent);
    }
}
