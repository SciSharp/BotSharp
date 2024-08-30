using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public IRoutingPlaner GetPlanner(Agent router)
    {
        var rule = router.RoutingRules.FirstOrDefault(x => x.Type == RuleType.Planner);

        var planner = _services.GetServices<IRoutingPlaner>().
            FirstOrDefault(x => x.GetType().Name.EndsWith(rule.Field));

        if (planner == null)
        {
            _logger.LogError($"Can't find specific planner named {rule.Field}");
            return _services.GetRequiredService<NaivePlanner>();
        }

        return planner;
    }
}
