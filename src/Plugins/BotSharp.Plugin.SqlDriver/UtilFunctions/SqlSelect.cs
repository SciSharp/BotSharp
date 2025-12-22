using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;
using System.Runtime;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.UtilFunctions;

public class SqlSelect : IFunctionCallback
{
    private readonly IServiceProvider _services;
    private readonly SqlDriverSetting _settings;
    public string Name => "util-db-sql_select";
    public string Indication => "Generated query statement. I'm pulling the data from database, please wait";

    public SqlSelect(IServiceProvider services, SqlDriverSetting setting)
    {
        _settings = setting;
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

        var dbHook = _services.GetRequiredService<IText2SqlHook>();
        var dbType = dbHook.GetDatabaseType(message);
        var dbConnectionString = dbHook.GetConnectionString(message) ??
            _settings.Connections.FirstOrDefault(c => c.DbType == dbType)?.ConnectionString ??
            throw new Exception("database connectdion is not found");
        

        var result = dbType switch
        {
            "mysql" => RunQueryInMySql(dbConnectionString, args),
            "sqlserver" or "mssql" => RunQueryInSqlServer(dbConnectionString, args),
            "redshift" => RunQueryInRedshift(dbConnectionString, args),
            "sqlite" => RunQueryInSqlite(dbConnectionString, [args.Statement]),
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

    private IEnumerable<dynamic> RunQueryInMySql(string connectionString, SqlStatement args)
    {
        using var connection = new MySqlConnection(connectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }

    private IEnumerable<dynamic> RunQueryInSqlServer(string connectionString, SqlStatement args)
    {
        using var connection = new SqlConnection(connectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }

    private IEnumerable<dynamic> RunQueryInSqlite(string connectionString, string[] sqlTexts)
    {
        using var connection = new SqliteConnection(connectionString);
        return connection.Query(sqlTexts[0]);
    }

    private IEnumerable<dynamic> RunQueryInRedshift(string connectionString, SqlStatement args)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var dictionary = new Dictionary<string, object>();
        foreach (var p in args.Parameters)
        {
            dictionary["@" + p.Name] = p.Value;
        }
        return connection.Query(args.Statement, dictionary);
    }
}
