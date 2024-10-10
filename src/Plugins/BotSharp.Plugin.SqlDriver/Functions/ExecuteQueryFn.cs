using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Core.Infrastructures;
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
        
        if (results.Count() == 0)
        {
            message.Content = "No record found";
            return true;
        }

        message.Content = JsonSerializer.Serialize(results);

        if (args.FormattingResult)
        {
            var conv = _services.GetRequiredService<IConversationService>();
            var sqlAgent = await _services.GetRequiredService<IAgentService>().LoadAgent(BuiltInAgentId.SqlDriver);
            var prompt = sqlAgent.Templates.FirstOrDefault(x => x.Name == "query_result_formatting");

            var completion = CompletionProvider.GetChatCompletion(_services,
                provider: sqlAgent.LlmConfig.Provider,
                model: sqlAgent.LlmConfig.Model);

            var result = await completion.GetChatCompletions(new Agent
            {
                Id = sqlAgent.Id,
                Instruction = prompt.Content,
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, message.Content)
            });

            message.Content = result.Content;
        }

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
        return connection.Query(string.Join("\r\n", sqlTexts));
    }
}
