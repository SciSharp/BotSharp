namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlUtilityHook : IAgentUtilityHook
{
    private const string SQL_TABLE_DEFINITION_FN = "sql_table_definition";
    private const string VERIFY_DICTIONARY_TERM_FN = "verify_dictionary_term";
    private const string SQL_SELECT_FN = "sql_select";

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
                Functions = new List<UtilityFunction> { new(SQL_SELECT_FN), new(SQL_TABLE_DEFINITION_FN) },
                Templates = new List<UtilityTemplate> { new($"sql_executor.fn") }
            }
        };

        utilities.AddRange(items);
    }
}
