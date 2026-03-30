namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlSelectFn : IFunctionCallback
{
    public string Name => "sql_select";
    private readonly IServiceProvider _services;
    private readonly SqlExecuteService _sqlExecuteService;

    public SqlSelectFn(IServiceProvider services, 
        SqlExecuteService sqlExecuteService)
    {
        _services = services;
        _sqlExecuteService = sqlExecuteService;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);

        if (args.GeneratedWithoutTableDefinition)
        {
            message.Content = $"Get the table definition first.";
            return false;
        }

        // check if need to instantely
        var dbHook = _services.GetRequiredService<IText2SqlHook>();
        var dbType = dbHook.GetDatabaseType(message);
        var dbConnectionString = dbHook.GetConnectionString(message) ?? 
            throw new Exception("database connectdion is not found");

        var result = await (dbType switch
        {
            "mysql" => _sqlExecuteService.RunQueryInMySql(dbConnectionString, args.Statement, args.Parameters),
            "sqlserver" or "mssql" => _sqlExecuteService.RunQueryInSqlServer(dbConnectionString, args.Statement, args.Parameters),
            "redshift" => _sqlExecuteService.RunQueryInRedshift(dbConnectionString, args.Statement, args.Parameters),
            "sqlite" => _sqlExecuteService.RunQueryInSqlite(dbConnectionString, args.Statement, args.Parameters),
            "mongodb" => _sqlExecuteService.RunQueryInMongoDb(dbConnectionString, args.Statement, args.Parameters),
            _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
        });

        if (result == null)
        {
            message.Content = "Record not found";
        }
        else
        {
            if (dbType == "mongodb") message.StopCompletion = true;
            message.Content = JsonSerializer.Serialize(result);
            args.Return.Value = message.Content;
        }
            
        return true;
    }
}
