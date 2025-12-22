using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using MongoDB.Driver;
using MongoDB.Bson;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlSelect : IFunctionCallback
{
    public string Name => "sql_select";
    private readonly IServiceProvider _services;
    private readonly SqlDriverSetting _settings;

    public SqlSelect(IServiceProvider services, SqlDriverSetting settings)
    {
        _settings = settings;
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
            "mongodb" => RunQueryInMongoDb(dbConnectionString, args),
            _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
        };

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

    private IEnumerable<dynamic> RunQueryInMongoDb(string connectionString, SqlStatement args)
    {
        var client = new MongoClient(connectionString);
        
        // Normalize multi-line query to single line
        var statement = Regex.Replace(args.Statement.Trim(), @"\s+", " ");
        
        // Parse MongoDB query: database.collection.find({query}).projection({}).sort({}).limit(100)
        var match = Regex.Match(statement, 
            @"^([^.]+)\.([^.]+)\.find\s*\((.*?)\)(.*)?$", 
            RegexOptions.Singleline);
        
        if (!match.Success)
            return ["Invalid MongoDB query format. Expected: database.collection.find({query})"];

        var queryJson = ApplyParameters(match.Groups[3].Value.Trim(), args.Parameters);

        try
        {
            var database = client.GetDatabase(match.Groups[1].Value);
            var collection = database.GetCollection<BsonDocument>(match.Groups[2].Value);
            
            var filter = string.IsNullOrWhiteSpace(queryJson) || queryJson == "{}" 
                ? Builders<BsonDocument>.Filter.Empty 
                : BsonDocument.Parse(queryJson);

            var findFluent = collection.Find(filter);
            findFluent = ApplyChainedOperations(findFluent, match.Groups[4].Value);

            return findFluent.ToList().Select(doc => BsonTypeMapper.MapToDotNetValue(doc));
        }
        catch (Exception ex)
        {
            return [$"Invalid MongoDB query: {ex.Message}"];
        }
    }

    private string ApplyParameters(string query, Models.SqlParameter[] parameters)
    {
        foreach (var p in parameters)
            query = query.Replace($"@{p.Name}", p.Value?.ToString() ?? "null");
        return query;
    }

    private IFindFluent<BsonDocument, BsonDocument> ApplyChainedOperations(
        IFindFluent<BsonDocument, BsonDocument> findFluent, string chainedOps)
    {
        if (string.IsNullOrWhiteSpace(chainedOps)) return findFluent;

        // Apply projection
        var projMatch = Regex.Match(chainedOps, @"\.projection\s*\((.*?)\)", RegexOptions.Singleline);
        if (projMatch.Success)
            findFluent = findFluent.Project<BsonDocument>(BsonDocument.Parse(projMatch.Groups[1].Value.Trim()));

        // Apply sort
        var sortMatch = Regex.Match(chainedOps, @"\.sort\s*\((.*?)\)", RegexOptions.Singleline);
        if (sortMatch.Success)
            findFluent = findFluent.Sort(BsonDocument.Parse(sortMatch.Groups[1].Value.Trim()));

        // Apply limit
        var limitMatch = Regex.Match(chainedOps, @"\.limit\s*\((\d+)\)");
        if (limitMatch.Success && int.TryParse(limitMatch.Groups[1].Value, out var limit))
        {
            findFluent = findFluent.Limit(limit);
        }
        else 
        {
            findFluent = findFluent.Limit(10);
        } 

        return findFluent;
    }

}
