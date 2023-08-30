namespace BotSharp.Plugin.PizzaBot.Hooks;

public class PizzaBotAgentHook : AgentHookBase
{
    public PizzaBotAgentHook(IServiceProvider services, AgentSettings settings)
    : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        dict["current_date"] = $"{DateTime.Now:MMM dd, yyyy}";
        dict["current_time"] = $"{DateTime.Now:hh:mm t}";
        dict["current_weekday"] = $"{DateTime.Now:dddd}";
        return true;
    }
}
