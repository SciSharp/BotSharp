using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDictionaryLookupHook : AgentHookBase, IAgentHook
{
    private const string SQL_EXECUTOR_TEMPLATE = "verify_dictionary_term.fn";
    private IEnumerable<string> _targetSqlExecutorFunctions = new List<string>
    {
        "verify_dictionary_term",
    };

    public override string SelfId => BuiltInAgentId.Planner;

    public SqlDictionaryLookupHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(Utility.SqlDictionaryLookup);

        if (isConvMode && isEnabled)
        {
            var (prompt, fns) = GetPromptAndFunctions();
            if (!fns.IsNullOrEmpty())
            {
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    agent.Instruction += $"\r\n\r\n{prompt}\r\n\r\n";
                }

                if (agent.Functions == null)
                {
                    agent.Functions = fns;
                }
                else
                {
                    agent.Functions.AddRange(fns);
                }
            }
        }

        base.OnAgentLoaded(agent);
    }

    private (string, List<FunctionDef>?) GetPromptAndFunctions()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var fns = agent?.Functions?.Where(x => _targetSqlExecutorFunctions.Contains(x.Name))?.ToList();

        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(SQL_EXECUTOR_TEMPLATE))?.Content ?? string.Empty;
        var dbType = GetDatabaseType();
        var render = _services.GetRequiredService<ITemplateRender>();
        prompt = render.Render(prompt, new Dictionary<string, object>
        {
            { "db_type", dbType }
        });

        return (prompt, fns);
    }

    private string GetDatabaseType()
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var dbType = "MySQL";

        if (!string.IsNullOrWhiteSpace(settings?.SqlServerConnectionString))
        {
            dbType = "SQL Server";
        }
        else if (!string.IsNullOrWhiteSpace(settings?.SqlLiteConnectionString))
        {
            dbType = "SQL Lite";
        }
        return dbType;
    }
}
