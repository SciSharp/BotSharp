namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerUtilityHook : IAgentUtilityHook
{
    private const string PREFIX = "util-audio-";
    private const string HANDLER_AUDIO = $"{PREFIX}handle_audio_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "audio",
            Name = UtilityName.AudioHandler,
            Items = [
                new UtilityItem
                {
                    FunctionName = HANDLER_AUDIO,
                    TemplateName = $"{HANDLER_AUDIO}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}