using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverAgentHook : AgentHookBase, IAgentHook
{
    public override string SelfId => BuiltInAgentId.Planner;

    public SqlDriverAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var dbType = SqlDriverHelper.GetDatabaseType(_services);
        agent.TemplateDict["db_type"] = dbType;
    }
}
