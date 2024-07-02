namespace BotSharp.Core.Files.Hooks;

internal class ImageGeneratorUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(AgentUtility.ImageGenerator);
    }
}