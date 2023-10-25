using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.PizzaBot.Hooks;

public class PizzaBotAgentHook : AgentHookBase
{
    public override string SelfId => "01fcc3e5-9af7-49e6-ad7a-a760bd12dc4a";

    public PizzaBotAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        return base.OnInstructionLoaded(template, dict);
    }
}
