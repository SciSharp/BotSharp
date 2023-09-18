using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing;

public class RoutingHook : AgentHookBase
{
    public RoutingHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agents = db.Agents.Where(x => !x.Disabled && x.AllowRouting).ToArray();

        var router = _services.GetRequiredService<IAgentRouting>();
        dict["routing_records"] = agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules.Where(x => x.Required)
                .Select(x => x.Field)
                .ToArray()
        }).ToArray();
        return true;
    }
}
