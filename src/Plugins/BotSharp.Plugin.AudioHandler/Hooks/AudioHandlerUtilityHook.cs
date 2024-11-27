namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerUtilityHook : IAgentUtilityHook
{
    private const string HANDLER_AUDIO = "handle_audio_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.AudioHandler,
            Functions = [new(HANDLER_AUDIO)],
            Templates = [new($"{HANDLER_AUDIO}.fn")]
        };

        utilities.Add(utility);
    }
}