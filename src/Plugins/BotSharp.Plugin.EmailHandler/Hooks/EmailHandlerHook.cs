using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Plugin.EmailHandler.Enums;

namespace BotSharp.Plugin.EmailHandler.Hooks;

public class EmailHandlerHook : AgentHookBase
{
    private static string EMAIL_READER_FN = "handle_email_reader";
    private static string EMAIL_SENDER_FN = "handle_email_sender";

    public override string SelfId => string.Empty;

    public EmailHandlerHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtilityLoadModel
        {
            UtilityName = UtilityName.EmailHandler,
            Content = new UtilityContent
            {
                Functions = [new(EMAIL_READER_FN), new(EMAIL_SENDER_FN)],
                Templates = [new($"{EMAIL_READER_FN}.fn"), new($"{EMAIL_SENDER_FN}.fn")]
            }
        };

        base.OnLoadAgentUtility(agent, [utilityLoad]);
        base.OnAgentLoaded(agent);
    }
}
