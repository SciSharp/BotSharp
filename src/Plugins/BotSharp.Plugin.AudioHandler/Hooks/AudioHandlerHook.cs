using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerHook : AgentHookBase, IAgentHook
{
    private const string HANDLER_AUDIO = "handle_audio_request";

    public override string SelfId => string.Empty;

    public AudioHandlerHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtility
        {
            Name = UtilityName.AudioHandler,
            Functions = [new(HANDLER_AUDIO)],
            Templates = [new($"{HANDLER_AUDIO}.fn")]
        };

        base.OnAgentLoaded(agent);
    }
}
