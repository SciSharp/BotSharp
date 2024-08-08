namespace BotSharp.Plugin.KnowledgeBase.Hooks;

public class KnowledgeBaseUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.KnowledgeRetrieval);
    }
}
