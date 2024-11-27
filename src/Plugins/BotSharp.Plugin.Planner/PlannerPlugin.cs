using BotSharp.Abstraction.Planning;
using BotSharp.Plugin.Planner.TwoStaging;

namespace BotSharp.Plugin.Planner;

/// <summary>
/// Plugin for AI Planning.
/// </summary>
public class PlannerPlugin : IBotSharpPlugin
{
    public string Id => "571f71fe-1583-46f2-b577-c8577a0a2903";
    public string Name => "AI Planning Plugin";
    public string Description => "Provide AI with different planning approaches to improve AI's ability to solve complex problems.";
    public string IconUrl => "https://e7.pngegg.com/pngimages/775/350/png-clipart-action-plan-computer-icons-plan-miscellaneous-text-thumbnail.png";

    public string[] AgentIds => [ BuiltInAgentId.Planner ];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITaskPlanner, TwoStageTaskPlanner>();
        services.AddScoped<IAgentHook, PlannerAgentHook>();
        services.AddScoped<IAgentUtilityHook, PlannerUtilityHook>();
    }
}
