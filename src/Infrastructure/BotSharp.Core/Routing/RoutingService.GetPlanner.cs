using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Routing.Enums;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Core.Routing.Planning;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public IPlaner GetPlanner(Agent router)
    {
        var planner = router.RoutingRules.FirstOrDefault(x => x.Type == RuleType.Planner);

        if (planner?.Field == nameof(HFPlanner))
            return _services.GetRequiredService<HFPlanner>();
        else if (planner?.Field == nameof(SequentialPlanner))
            return _services.GetRequiredService<SequentialPlanner>();
        else
            return _services.GetRequiredService<NaivePlanner>();
    }
}
