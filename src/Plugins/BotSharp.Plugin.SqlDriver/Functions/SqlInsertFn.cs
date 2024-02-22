using BotSharp.Plugin.SqlDriver.Models;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlInsertFn : IFunctionCallback
{
    public string Name => "sql_insert";
    private readonly IServiceProvider _services;

    public SqlInsertFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();

        // Check duplication
        if (sqlDriver.Statements.Exists(x => x.Statement == args.Statement))
        {
            var list = sqlDriver.Statements.Where(x => x.Statement == args.Statement).ToList();
            foreach (var statement in list)
            {
                var p1 = string.Join(", ", statement.Parameters.OrderBy(x => x.Name).Select(x => x.Value));
                var p2 = string.Join(", ", args.Parameters.OrderBy(x => x.Name).Select(x => x.Value));
                if (p1 == p2)
                {
                    message.Content = "Skip duplicated INSERT statement";
                    return false;
                }
            }
        }
        
        sqlDriver.Enqueue(args);
        message.Content = $"Inserted new record successfully.";
        if (args.Return != null)
        {
            /*sqlDriver.Enqueue(new SqlStatement
            {
                Statement = $"SELECT LAST_INSERT_ID() INTO @{args.Return.Alias};",
                Reason = $"select auto-incremented id into '{args.Return.Alias}'"
            });*/
            message.Content += $" The {args.Return.Name} is saved to @{args.Return.Alias}.";
        }
        
        return true;
    }
}
