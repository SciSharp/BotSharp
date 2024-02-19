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
        if (sqlDriver.Statements.Exists(x => x.Statement == args.Statement))
        {
            message.Content = "Skipped duplicated statement.";
            return false;
        }
        sqlDriver.Enqueue(args);
        message.Content = $"Inserted new record {JsonSerializer.Serialize(args.Parameters)} successfully";
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
