namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.SqlExecutor);
        utilities.Add(UtilityName.SqlDictionaryLookup);
        utilities.Add(UtilityName.SqlTableDefinition);
    }
}
