using BotSharp.Core.Infrastructures;
using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class ExecuteQueryFn : IFunctionCallback
{
    public string Name => "execute_sql";
    public string Indication => "Performing data retrieval operation.";
    private readonly SqlDriverSetting _setting;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public ExecuteQueryFn(IServiceProvider services, SqlDriverSetting setting, ILogger<ExecuteQueryFn> logger)
    {
        _services = services;
        _setting = setting;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExecuteQueryArgs>(message.FunctionArgs);
        //var refinedArgs = await RefineSqlStatement(message, args);
        var dbHook = _services.GetRequiredService<ISqlDriverHook>();
        var dbType = dbHook.GetDatabaseType(message);

        try
        {
            var results = dbType.ToLower() switch
            {
                "mysql" => RunQueryInMySql(args.SqlStatements),
                "sqlserver" => RunQueryInSqlServer(args.SqlStatements),
                "redshift" => RunQueryInRedshift(args.SqlStatements),
                _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
            };

            if (args.SqlStatements.Length == 1 && args.SqlStatements[0].StartsWith("DROP TABLE"))
            {
                message.Content = "Drop table successfully";
                return true;
            }

            if (results.Count() == 0)
            {
                message.Content = "No record found";
                return true;
            }

            message.Content = JsonSerializer.Serialize(results);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Error occurred while executing SQL query.");
            message.Content = $"Error occurred while executing SQL query: {ex.Message}";
            message.Data = ex;
            message.StopCompletion = true;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing SQL query.");
            message.Content = $"Error occurred while executing SQL query: {ex.Message}";
            message.StopCompletion = true;
            return false;
        }

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
            message.StopCompletion = true;
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

    private IEnumerable<dynamic> RunQueryInRedshift(string[] sqlTexts)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new NpgsqlConnection(settings.RedshiftConnectionString);
        return connection.Query(string.Join("\r\n", sqlTexts));
    }

    private async Task<ExecuteQueryArgs> RefineSqlStatement(RoleDialogModel message, ExecuteQueryArgs args)
    {
        if (args.Tables == null || args.Tables.Length == 0)
        {
            return args;
        }

        // get table DDL
        var fn = _services.GetRequiredService<IRoutingService>();
        var msg = RoleDialogModel.From(message);
        await fn.InvokeFunction("sql_table_definition", msg);

        // refine SQL
        var agentService = _services.GetRequiredService<IAgentService>();
        var currentAgent = await agentService.LoadAgent(message.CurrentAgentId);
        var dictionarySqlPrompt = await GetDictionarySQLPrompt(string.Join("\r\n\r\n", args.SqlStatements), msg.Content);
        var agent = new Agent
        {
            Id = message.CurrentAgentId ?? string.Empty,
            Name = "sqlDriver_ExecuteQuery",
            Instruction = dictionarySqlPrompt,
            TemplateDict = new Dictionary<string, object>(),
            LlmConfig = currentAgent.LlmConfig
        };

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: agent.LlmConfig.Provider,
            model: agent.LlmConfig.Model);

        var refinedMessage = await completion.GetChatCompletions(agent, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, "Check and output the correct SQL statements")
        });

        return refinedMessage.Content.JsonContent<ExecuteQueryArgs>();
    }

    private async Task<string> GetDictionarySQLPrompt(string originalSql, string tableStructure)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        var agent = await agentService.GetAgent(BuiltInAgentId.SqlDriver);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "sql_statement_correctness")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new ExecuteQueryArgs { });

        return render.Render(template, new Dictionary<string, object>
        {
            { "original_sql", originalSql },
            { "table_structure", tableStructure },
            { "response_format", responseFormat }
        });
    }
}
