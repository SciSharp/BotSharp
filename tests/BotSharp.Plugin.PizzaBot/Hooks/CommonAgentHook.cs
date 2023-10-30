using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.PizzaBot.Hooks;

public class CommonAgentHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    public CommonAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        dict["current_date"] = DateTime.Now.ToString("MM/dd/yyyy");
        dict["current_time"] = DateTime.Now.ToString("hh:mm tt");
        dict["current_weekday"] = DateTime.Now.DayOfWeek;
        return base.OnInstructionLoaded(template, dict);
    }
}
