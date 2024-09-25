namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(Utility.SqlExecutor);
        utilities.Add(Utility.SqlDictionaryLookup);
    }
}
