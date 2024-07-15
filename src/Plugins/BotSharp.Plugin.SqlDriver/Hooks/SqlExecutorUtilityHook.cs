namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlExecutorUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(Utility.SqlExecutor);
    }
}
