namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeBaseUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-knowledge-";
    private static string KNOWLEDGE_RETRIEVAL_FN = $"{PREFIX}knowledge_retrieval";

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
