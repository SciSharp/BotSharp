using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlExecutorHook : AgentHookBase, IAgentHook
{
    private const string SQL_EXECUTOR_TEMPLATE = "sql_executor.fn";
    private IEnumerable<string> _targetSqlExecutorFunctions = new List<string>
    {
        "sql_select"
    };

    public override string SelfId => string.Empty;

    public SqlExecutorHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        var isEnabled = !agent.Utilities.IsNullOrEmpty() && agent.Utilities.Contains(Utility.SqlExecutor);

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
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(SQL_EXECUTOR_TEMPLATE))?.Content ?? string.Empty;
        var fns = agent?.Functions?.Where(x => _targetSqlExecutorFunctions.Contains(x.Name))?.ToList();
        return (prompt, fns);
    }
}
