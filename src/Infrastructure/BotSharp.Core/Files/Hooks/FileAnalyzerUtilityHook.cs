namespace BotSharp.Core.Files.Hooks;

public class FileAnalyzerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(AgentUtility.FileAnalyzer);
    }
}
