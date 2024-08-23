using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        // get agent service
        var agentService = _services.GetRequiredService<IAgentService>();

        // var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();

        //get table DDL from database
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        var dictionary = new Dictionary<string, object>();

        var table_ddl = "";
        foreach (var p in (List<string>)message.Data)
        {
            dictionary["@" + "table_name"] = p;
            var escapedTableName = MySqlHelper.EscapeString(p);
            dictionary["table_name"] = escapedTableName;
            // can you replace this with a parameterized query?
            var sql = $"select * from information_schema.tables where table_name ='{dictionary["table_name"]}'";

            var result = connection.QueryFirstOrDefault(sql: sql, dictionary);
            if (result != null)
            {
                sql = $"SHOW CREATE TABLE `{dictionary["table_name"]}`";
                result = connection.QueryFirstOrDefault(sql: sql, dictionary);
                table_ddl += "\r\n" + result;
            }
            
        }
        message.Content = table_ddl;

        return true;
    }
}
