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
            Name = "routing.tools",
            Functions = [new($"{REDIRECT_TO_AGENT}"), new($"{FALLBACK_TO_ROUTER}")],
            Templates = [new($"{REDIRECT_TO_AGENT}.fn"), new($"{FALLBACK_TO_ROUTER}.fn")]
        });
    }
}
