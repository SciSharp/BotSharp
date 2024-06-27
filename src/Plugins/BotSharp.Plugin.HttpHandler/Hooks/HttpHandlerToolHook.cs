using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.HttpHandler.Enums;

namespace BotSharp.Plugin.HttpHandler.Hooks;

public class HttpHandlerToolHook : IAgentToolHook
{
    public void AddTools(List<string> tools)
    {
        tools.Add(Tool.HttpHandler);
    }
}
