using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class GetTableDefinitionFn : IFunctionCallback
{
    public string Name => "get_table_definition";
    private readonly IServiceProvider _services;

    public GetTableDefinitionFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();

        // Get table DDL from database
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        var dictionary = new Dictionary<string, object>();
        var tableDdls = new List<string>();

        foreach (var p in (List<string>)message.Data)
        {
            var escapedTableName = MySqlHelper.EscapeString(p);
            dictionary["@" + "table_name"] = p;
            dictionary["table_name"] = escapedTableName;

            var sql = $"select * from information_schema.tables where table_name ='{escapedTableName}'";
            var result = connection.QueryFirstOrDefault(sql: sql, dictionary);
            if (result == null) continue;

            sql = $"SHOW CREATE TABLE `{escapedTableName}`";
            result = connection.QueryFirstOrDefault(sql: sql, dictionary);
            tableDdls.Add(result);
        }

        message.Content = string.Join("\r\n", tableDdls);
        return true;
    }
}
