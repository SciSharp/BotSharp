using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.HttpHandler);
    }
}
