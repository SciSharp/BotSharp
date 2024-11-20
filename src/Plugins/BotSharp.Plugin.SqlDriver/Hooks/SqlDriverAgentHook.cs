using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverAgentHook : AgentHookBase, IAgentHook
{
    private const string SQL_TABLE_DEFINITION_FN = "sql_table_definition";
    private const string VERIFY_DICTIONARY_TERM_FN = "verify_dictionary_term";
    private const string SQL_SELECT_FN = "sql_select";

    public override string SelfId => BuiltInAgentId.Planner;

    public SqlDriverAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var dbType = SqlDriverHelper.GetDatabaseType(_services);
        var promptData = new Dictionary<string, object>
        {
            { "db_type", dbType }
        };

        var utilityLoads = new List<AgentUtilityLoadModel>
        {
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.SqlTableDefinition,
                Content = new UtilityContent
                {
                    Functions = new List<UtilityFunction> { new(SQL_TABLE_DEFINITION_FN) },
                    Templates = new List<UtilityTemplate> { new($"{SQL_TABLE_DEFINITION_FN}.fn", promptData) }
                }
            },
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.SqlDictionaryLookup,
                Content = new UtilityContent
                {
                    Functions = new List<UtilityFunction> { new(VERIFY_DICTIONARY_TERM_FN) },
                    Templates = new List<UtilityTemplate> { new($"{VERIFY_DICTIONARY_TERM_FN}.fn", promptData) }
                }
            },
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.SqlExecutor,
                Content = new UtilityContent
                {
                    Functions = new List<UtilityFunction> { new(SQL_SELECT_FN), new(SQL_TABLE_DEFINITION_FN) },
                    Templates = new List<UtilityTemplate> { new($"sql_executor.fn", promptData) }
                }
            }
        };

        base.OnLoadAgentUtility(agent, utilityLoads);
        base.OnAgentLoaded(agent);
    }
}
