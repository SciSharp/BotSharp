using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Hooks;

public class RoutingAgentHook : AgentHookBase
{
    private readonly RoutingSettings _routingSetting;
    public override string SelfId => string.Empty;

    public RoutingAgentHook(IServiceProvider services, AgentSettings settings, RoutingSettings routingSetting) 
        : base(services, settings)
    {
        _routingSetting = routingSetting;
    }

    public override async Task<bool> OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        if (_agent.Type != AgentType.Routing)
        {
            return await base.OnInstructionLoaded(template, dict);
        }
        dict["router"] = _agent;

        var routing = _services.GetRequiredService<IRoutingService>();
        var agents = await routing.GetRoutableAgents(_agent.Profiles);

        // filter Planner
        var planningRule = _agent.RoutingRules.FirstOrDefault(x => x.Type == "planner");
        if (planningRule != null)
        {
            var planners = agents.Where(x => x.Type == AgentType.Planning && x.Name != planningRule.Field).ToArray();
            agents = agents.Except(planners).ToArray();
        }

        // GetRoutableAgents is cached and shared by every conversation: build this conversation's view
        // without the fields it already has, instead of removing them from the cached agents.
        var states = _services.GetRequiredService<IConversationStateService>();
        agents = agents.Select(x => new RoutableAgent
        {
            AgentId = x.AgentId,
            Name = x.Name,
            Description = x.Description,
            Type = x.Type,
            Profiles = x.Profiles,
            RequiredFields = x.RequiredFields.Where(f => !states.ContainsState(f.Name)).ToList(),
            OptionalFields = x.OptionalFields.Where(f => !states.ContainsState(f.Name)).ToList()
        }).ToArray();

        dict["routing_agents"] = agents;

        return await base.OnInstructionLoaded(template, dict);
    }

    public override async Task<bool> OnFunctionsLoaded(List<FunctionDef> functions)
    {
        if (_agent.Type == AgentType.Task)
        {
            // check if enabled the routing rule
            var routing = _services.GetRequiredService<IRoutingService>();
            var rules = (await routing.GetRulesByAgentId(_agent.Id)).ToList();
            if (!rules.IsNullOrEmpty())
            {
                await HookEmitter.Emit<IRoutingHook>(_services, async hook => await hook.OnRoutingRulesLoaded(_agent.Id, rules), _agent.Id);
            }
            var rule = rules.FirstOrDefault(x => x.Type == RuleType.Fallback);
            if (rule != null)
            {
                var agentService = _services.GetRequiredService<IAgentService>();
                var redirectAgent = await agentService.GetAgent(rule.RedirectTo);

                var json = JsonSerializer.Serialize(new
                {
                    user_goal_agent = new
                    {
                        type = "string",
                        description = $"{_agent.Name}"
                    },
                    next_action_agent = new
                    {
                        type = "string",
                        description = $"{redirectAgent.Name}"
                    },
                    reason = new
                    {
                        type = "string",
                        description = $"the reason why you need to fallback to [{redirectAgent.Name}] from [{_agent.Name}]"
                    },
                });
                functions.Add(new FunctionDef
                {
                    Name = "fallback_to_router",
                    Description = $"If the user's request is beyond your capabilities, you can call this function to handle by other agent ({redirectAgent.Name}).",
                    Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(json),
                        Required = new List<string>
                        {
                            "user_goal_agent",
                            "next_action_agent",
                            "reason"
                        }
                    }
                });
            }
        }

        return await base.OnFunctionsLoaded(functions);
    }
}
