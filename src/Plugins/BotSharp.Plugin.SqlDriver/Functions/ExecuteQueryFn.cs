using BotSharp.Plugin.SqlDriver.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class ExecuteQueryFn : IFunctionCallback
{
    public string Name => "execute_sql";
    public string Indication => "Performing data retrieval operation.";
    private readonly SqlDriverSetting _setting;
    private readonly IServiceProvider _services;

    public ExecuteQueryFn(IServiceProvider services, SqlDriverSetting setting)
    {
        _services = services;
        _setting = setting;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExecuteQueryArgs>(message.FunctionArgs);
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var results = settings.DatabaseType switch
        {
            "MySql" => RunQueryInMySql(args.SqlStatements),
            "SqlServer" => RunQueryInSqlServer(args.SqlStatements),
            _ => throw new NotImplementedException($"Database type {settings.DatabaseType} is not supported.")
        };

        message.Content = JsonSerializer.Serialize(results);
        return true;
    }

    private IEnumerable<dynamic> RunQueryInMySql(string[] sqlTexts)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString ?? settings.MySqlConnectionString);
        return connection.Query(string.Join(";\r\n", sqlTexts));
    }

    private IEnumerable<dynamic> RunQueryInSqlServer(string[] sqlTexts)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new SqlConnection(settings.SqlServerExecutionConnectionString ?? settings.SqlServerConnectionString);
        var dictionary = new Dictionary<string, object>();
        return connection.Query(string.Join("\r\n", sqlTexts));
    }
}
