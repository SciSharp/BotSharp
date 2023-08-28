namespace BotSharp.Core.Hooks;

public class ReasoningHook : AgentHookBase
{
    public ReasoningHook(IServiceProvider services, AgentSettings settings)
    : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        return true;
    }
}
