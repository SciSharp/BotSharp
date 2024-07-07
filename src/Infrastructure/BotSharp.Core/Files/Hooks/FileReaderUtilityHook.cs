namespace BotSharp.Core.Files.Hooks;

public class FileReaderUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(AgentUtility.FileReader);
    }
}
