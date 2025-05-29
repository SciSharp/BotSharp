namespace BotSharp.Core.Routing.Hooks;

public class RoutingUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-routing-";
    private static string REDIRECT_TO_AGENT = $"{PREFIX}redirect_to_agent";
    private static string FALLBACK_TO_ROUTER = $"{PREFIX}fallback_to_router";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        utilities.Add(new AgentUtility
        {
            Category = "routing",
            Name = "routing.tools",
            Items = [
                new UtilityItem
                {
                    FunctionName = $"{REDIRECT_TO_AGENT}",
                    TemplateName = $"{REDIRECT_TO_AGENT}.fn"
                },
                new UtilityItem
                {
                    FunctionName = $"{FALLBACK_TO_ROUTER}",
                    TemplateName = $"{FALLBACK_TO_ROUTER}.fn"
                }
            ]
        });
    }
}
