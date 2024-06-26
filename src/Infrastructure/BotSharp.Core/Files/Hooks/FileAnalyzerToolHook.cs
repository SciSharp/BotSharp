
namespace BotSharp.Core.Files.Hooks;

public class FileAnalyzerToolHook : IAgentToolHook
{
    public void AddTools(List<string> tools)
    {
        tools.Add(AgentTool.FileAnalyzer);
    }
}
