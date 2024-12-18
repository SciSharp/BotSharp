using BotSharp.Plugin.Planner.SqlGeneration.Models;
using BotSharp.Plugin.Planner.TwoStaging;
using BotSharp.Plugin.Planner.TwoStaging.Models;

namespace BotSharp.Plugin.Planner.SqlGeneration.Functions;

public class SqlReviewFn : IFunctionCallback
{
    public string Name => "sql_review";
    public string Indication => "Currently reviewing SQL statement";

    private readonly IServiceProvider _services;
    private readonly ILogger<SqlReviewFn> _logger;

    public SqlReviewFn(
        IServiceProvider services,
        ILogger<SqlReviewFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlReviewArgs>(message.FunctionArgs);
        if (!message.Content.StartsWith("```sql"))
        {
            message.Content = $"```sql\r\n{args.SqlStatement}\r\n```";
        }
        if (args != null && !args.IsSqlTemplate && args.ContainsSqlStatements)
        {
            await HookEmitter.Emit<IPlanningHook>(_services, async hook =>
                await hook.OnSourceCodeGenerated(nameof(TwoStageTaskPlanner), message, "sql")
            );
        }
        return true;
    }
}
