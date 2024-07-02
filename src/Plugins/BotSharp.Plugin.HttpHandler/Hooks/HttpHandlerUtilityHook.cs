using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.HttpHandler.Enums;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(Utility.HttpHandler);
    }
}
