using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.EmailHandler.Enums;

namespace BotSharp.Plugin.EmailHandler.Hooks;

public class EmailHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.EmailHandler);
    }
}
