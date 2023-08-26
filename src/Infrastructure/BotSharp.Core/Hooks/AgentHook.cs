namespace BotSharp.Core.Hooks;

public class AgentHook : AgentHookBase
{
    public AgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        var router = _services.GetRequiredService<IAgentRouting>();
        dict["routing_records"] = router.GetRoutingRecords();
        return true;
    }
}
