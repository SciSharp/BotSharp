using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

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
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        var tables = new string[] { args.Table };
        var agentService = _services.GetRequiredService<IAgentService>();
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();

        // Get table DDL from database

        var tableDdls = new List<string>();
        using var connection = new MySqlConnection(settings.MySqlExecutionConnectionString);
        connection.Open();

        foreach (var table in tables)
        {
            try
            {
                var escapedTableName = MySqlHelper.EscapeString(table);
                var sql = $"SHOW CREATE TABLE `{escapedTableName}`";

                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var result = reader.GetString(1);
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
