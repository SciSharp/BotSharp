using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerUtilityHook : IAgentUtilityHook
{
    private static string HTTP_HANDLER_FN = "handle_http_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.HttpHandler,
            Functions = [new(HTTP_HANDLER_FN)],
            Templates = [new($"{HTTP_HANDLER_FN}.fn")]
        };

        utilities.Add(utility);
    }
}
