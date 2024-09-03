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
        var tableDdls = new List<string>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        connection.Open();

        foreach (var table in (List<string>)message.Data)
        {
            var escapedTableName = MySqlHelper.EscapeString(table);

            var sql = $"select * from information_schema.tables where table_name = @tableName";
            var result = connection.QueryFirstOrDefault(sql, new
            {
                tableName = escapedTableName
            });

            if (result == null) continue;

            sql = $"SHOW CREATE TABLE `{escapedTableName}`";
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                result = reader.GetString(1);
                tableDdls.Add(result);
            }

            reader.Close();
            command.Dispose();
        }

        connection.Close();
        message.Content = string.Join("\r\n", tableDdls);
        return true;
    }
}
