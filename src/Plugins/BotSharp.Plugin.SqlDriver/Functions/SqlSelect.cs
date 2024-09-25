using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlSelect : IFunctionCallback
{
    public string Name => "sql_select";
    private readonly IServiceProvider _services;

    public SqlSelect(IServiceProvider services)
    {
        _services = services;
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
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var result = settings.DatabaseType switch
        {
            "MySql" => RunQueryInMySql(args),
            "SqlServer" => RunQueryInSqlServer(args),
            _ => throw new NotImplementedException($"Database type {settings.DatabaseType} is not supported.")
        };

        if (result == null)
        {
            message.Content = "Record not found";
        }
        else
        {
            message.Content = JsonSerializer.Serialize(result);
            args.Return.Value = message.Content;
        }
            
        return true;
    }

    private IEnumerable<dynamic> RunQueryInMySql(SqlStatement args)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }

    private IEnumerable<dynamic> RunQueryInSqlServer(SqlStatement args)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new SqlConnection(settings.SqlServerExecutionConnectionString ?? settings.SqlServerConnectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }
}
