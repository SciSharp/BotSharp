namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlUtilityHook : IAgentUtilityHook
{
    private const string PREFIX = "util-db-";
    private const string SQL_TABLE_DEFINITION_FN = $"{PREFIX}sql_table_definition";
    private const string VERIFY_DICTIONARY_TERM_FN = $"{PREFIX}verify_dictionary_term";
    private const string SQL_SELECT_FN = $"{PREFIX}sql_select";
    private const string SQL_EXECUTOR_FN = $"{PREFIX}sql_executor";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Name = UtilityName.SqlTableDefinition,
                Functions = [new(SQL_TABLE_DEFINITION_FN)],
                Templates = [new($"{SQL_TABLE_DEFINITION_FN}.fn")]
            },
            new AgentUtility
            {
                Name = UtilityName.SqlDictionaryLookup,
                Functions = [new(VERIFY_DICTIONARY_TERM_FN)],
                Templates = [new($"{VERIFY_DICTIONARY_TERM_FN}.fn")]
            },
            new AgentUtility
            {
                Name = UtilityName.SqlExecutor,
                Functions = [new(SQL_SELECT_FN), new(SQL_TABLE_DEFINITION_FN)],
                Templates = [new($"{SQL_EXECUTOR_FN}.fn")]
            }
        };

        utilities.AddRange(items);
    }
}
