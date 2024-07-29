namespace BotSharp.Plugin.Planner;

public class PlannerPlugin : IBotSharpPlugin
{
    public string Id => "571f71fe-1583-46f2-b577-c8577a0a2903";
    public string Name => "AI Planning Plugin";
    public string Description => "Provide AI with different planning approaches to improve AI's ability to solve complex problems.";
    public string IconUrl => "https://library.ucf.edu/wp-content/uploads/sites/5/2015/03/SC-Planning-Icon-300x290.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IAgentHook, PlannerAgentHook>();
        services.AddScoped<IAgentUtilityHook, PlannerUtilityHook>();
    }
}
