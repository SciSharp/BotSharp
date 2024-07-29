namespace BotSharp.Plugin.Planner.TwoStaging;

public partial class TwoStageTaskPlanner : ITaskPlanner
{
    private readonly IServiceProvider _services;

    public TwoStageTaskPlanner(IServiceProvider services)
    {
        _services = services;
    }
}
