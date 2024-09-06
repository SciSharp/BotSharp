namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.AudioHandler);
    }
}