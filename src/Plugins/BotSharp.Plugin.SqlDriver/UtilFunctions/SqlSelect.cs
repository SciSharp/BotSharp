using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.UtilFunctions;

public class SqlSelect : IFunctionCallback
{
    private readonly IServiceProvider _services;
    public string Name => "util-db-sql_select";
    public string Indication => "Extracting data";

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

        var dbType = args.DBProvider.ToLowerInvariant();

        var result = dbType switch
        {
            "mysql" => RunQueryInMySql(args),
            "sqlserver" or "mssql" => RunQueryInSqlServer(args),
            "redshift" => RunQueryInRedshift(args),
            _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
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

    private IEnumerable<dynamic> RunQueryInRedshift(SqlStatement args)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new NpgsqlConnection(settings.RedshiftConnectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }
}
