using Microsoft.Extensions.Logging;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class GetTableDefinitionFn : IFunctionCallback
{
    public string Name => "get_table_definition";
    private readonly IServiceProvider _services;
    private readonly ILogger<GetTableDefinitionFn> _logger;

    public GetTableDefinitionFn(
        IServiceProvider services,
        ILogger<GetTableDefinitionFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();

        // Get table DDL from database
        var tables = message.Data as IEnumerable<string>;
        if (tables.IsNullOrEmpty()) return false;

        var tableDdls = new List<string>();
        using var connection = new MySqlConnection(settings.MySqlConnectionString);
        connection.Open();

        foreach (var table in tables)
        {
            try
            {
                var sql = $"select * from information_schema.tables where table_name = @tableName";
                var escapedTableName = MySqlHelper.EscapeString(table);

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
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when getting ddl statement of table {table}. {ex.Message}\r\n{ex.InnerException}");
            }
        }

        connection.Close();
        message.Content = string.Join("\r\n\r\n", tableDdls);
        return true;
    }
}
