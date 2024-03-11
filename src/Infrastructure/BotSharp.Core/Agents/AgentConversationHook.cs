
namespace BotSharp.Core.Agents;

public class AgentConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;

    public AgentConversationHook(IServiceProvider services)
    {
        _services = services;
    }

    public override async Task OnStateChanged(string name, string preValue, string currentValue)
    {
        // Apply new states to agent TemplateDict
        var routing = _services.GetRequiredService<IRoutingContext>();
        var agentId = routing.GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return;
        }
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = agentService.LoadAgent(agentId).Result;
        agent.TemplateDict[name] = currentValue;
    }
}
