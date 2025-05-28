namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeBaseUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-kg-";
    private static string KNOWLEDGE_RETRIEVAL_FN = $"{PREFIX}knowledge_retrieval";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "knowledge",
            Name = UtilityName.KnowledgeRetrieval,
            Items = [
                new UtilityItem
                {
                    FunctionName = KNOWLEDGE_RETRIEVAL_FN,
                    TemplateName = $"{KNOWLEDGE_RETRIEVAL_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}
