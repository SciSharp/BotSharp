namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeBaseUtilityHook : IAgentUtilityHook
{
    private const string KNOWLEDGE_RETRIEVAL_FN = "knowledge_retrieval";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.KnowledgeRetrieval,
            Functions = [new(KNOWLEDGE_RETRIEVAL_FN)],
            Templates = [new($"{KNOWLEDGE_RETRIEVAL_FN}.fn")]
        };

        utilities.Add(utility);
    }
}
