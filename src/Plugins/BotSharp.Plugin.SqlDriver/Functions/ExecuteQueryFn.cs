using BotSharp.Core.Infrastructures;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using System.Text;

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
        var dbConnectionString = dbHook.GetConnectionString(message);

        // Print all the SQL statements for debugging
        _logger.LogInformation("Executing SQL Statements: {SqlStatements}", string.Join("\r\n", args.SqlStatements));

        IEnumerable<dynamic> results = [];
        try
        {
            results = dbType.ToLower() switch
            {
                "mysql" => RunQueryInMySql(args.SqlStatements),
                "sqlserver" or "mssql" => RunQueryInSqlServer(args.SqlStatements),
                "redshift" => RunQueryInRedshift(args.SqlStatements),
                "sqlite" => RunQueryInSqlite(dbConnectionString, args.SqlStatements),
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

            message.Content = FormatResultsToCsv(results);
            message.Data = results;
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

        /*var conv = _services.GetRequiredService<IConversationService>();
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

        message.Content = result.Content;*/

        if (args.ResultFormat.ToLower() == "markdown")
        {
            message.Content = FormatResultsToMarkdown(results);
        }
        else if (args.ResultFormat.ToLower() == "csv")
        {
            message.Content = FormatResultsToCsv(results);
        }
        message.StopCompletion = true;

        return true;
    }

    private string FormatResultsToCsv(IEnumerable<dynamic> results)
    {
        if (results == null || !results.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var firstRow = results.First() as IDictionary<string, object>;
        
        if (firstRow == null)
        {
            return string.Empty;
        }

        // Write CSV header
        var headers = firstRow.Keys.ToList();
        sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsvField(h))));

        // Write CSV rows
        foreach (var row in results)
        {
            var rowDict = row as IDictionary<string, object>;
            if (rowDict != null)
            {
                var values = headers.Select(h => 
                {
                    var value = rowDict.ContainsKey(h) ? rowDict[h] : null;
                    return EscapeCsvField(value?.ToString() ?? string.Empty);
                });
                sb.AppendLine(string.Join(",", values));
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private string FormatResultsToMarkdown(IEnumerable<dynamic> results)
    {
        if (results == null || !results.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var firstRow = results.First() as IDictionary<string, object>;
        
        if (firstRow == null)
        {
            return string.Empty;
        }

        var headers = firstRow.Keys.ToList();

        // Write Markdown table header
        sb.AppendLine("| " + string.Join(" | ", headers) + " |");
        
        // Write separator row
        sb.AppendLine("|" + string.Join("|", headers.Select(_ => "-------")) + "|");

        // Write data rows
        foreach (var row in results)
        {
            var rowDict = row as IDictionary<string, object>;
            if (rowDict != null)
            {
                var values = headers.Select(h => 
                {
                    var value = rowDict.ContainsKey(h) ? rowDict[h] : null;
                    return EscapeMarkdownField(value?.ToString() ?? string.Empty);
                });
                sb.AppendLine("| " + string.Join(" | ", values) + " |");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string EscapeMarkdownField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // Escape pipe characters which are special in Markdown tables
        return field.Replace("|", "\\|");
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

    private IEnumerable<dynamic> RunQueryInSqlite(string connectionString, string[] sqlTexts)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        using var connection = new SqliteConnection(connectionString);
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
