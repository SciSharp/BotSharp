using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerHook : AgentHookBase
{
    private static string HTTP_HANDLER_FN = "handle_http_request";

    public override string SelfId => string.Empty;

    public HttpHandlerHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtilityLoadModel
        {
            UtilityName = UtilityName.HttpHandler,
            Content = new UtilityContent
            {
                Functions = [new(HTTP_HANDLER_FN)],
                Templates = [new($"{HTTP_HANDLER_FN}.fn")]
            }
        };

        base.OnLoadAgentUtility(agent, [utilityLoad]);
        base.OnAgentLoaded(agent);
    }
}
