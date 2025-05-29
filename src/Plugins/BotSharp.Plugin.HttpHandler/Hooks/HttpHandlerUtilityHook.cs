using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-http-";
    private static string HTTP_HANDLER_FN = $"{PREFIX}handle_http_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "http",
            Name = UtilityName.HttpHandler,
            Items = [
                new UtilityItem
                {
                    FunctionName = HTTP_HANDLER_FN,
                    TemplateName = $"{HTTP_HANDLER_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}
