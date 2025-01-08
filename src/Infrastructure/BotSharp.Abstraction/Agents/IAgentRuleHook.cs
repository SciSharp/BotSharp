namespace BotSharp.Abstraction.Agents;

public interface IAgentRuleHook
{
    void AddRules(List<AgentRule> rules);
}
