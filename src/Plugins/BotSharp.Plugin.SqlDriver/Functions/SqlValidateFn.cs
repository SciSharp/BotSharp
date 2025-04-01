namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlValidateFn : IFunctionCallback
{
    public string Name => "validate_sql";
    public string Indication => "Performing data validate operation.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public SqlValidateFn(IServiceProvider services, ILogger<SqlValidateFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        // remove comments start with "--"
        string pattern = @"--.*";
        string sql = Regex.Replace(message.Content, pattern, string.Empty);

        pattern = @"```sql\s*([\s\S]*?)\s*```";
        sql = Regex.Match(sql, pattern)?.Value;

        if (!Regex.IsMatch(sql, pattern))
        {
            return false;
        }

        sql = Regex.Match(sql, pattern).Groups[1].Value;
        message.Content = sql;

        var dbHook = _services.GetRequiredService<ISqlDriverHook>();
        var dbType = dbHook.GetDatabaseType(message);
        var validateSql = dbType.ToLower() switch
        {
            "mysql" => $"EXPLAIN\r\n{sql.Replace("SET ", "-- SET ", StringComparison.InvariantCultureIgnoreCase).Replace(";", "; EXPLAIN ").TrimEnd("EXPLAIN ".ToCharArray())}",
            "sqlserver" => $"SET PARSEONLY ON;\r\n{sql}\r\nSET PARSEONLY OFF;",
            "redshift" => $"explain\r\n{sql}",
            _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
        };
        var msgCopy = RoleDialogModel.From(message);
        msgCopy.FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
        {
            SqlStatements = [validateSql]
        });

        var fn = _services.GetRequiredService<IRoutingService>();
        await fn.InvokeFunction("execute_sql", msgCopy);

        if (msgCopy.Data != null && msgCopy.Data is DbException ex)
        {
            
            var instructService = _services.GetRequiredService<IInstructService>();
            var agentService = _services.GetRequiredService<IAgentService>();
            var states = _services.GetRequiredService<IConversationStateService>();

            var query = "Correct SQL Statement and keep the comments/explanations";
            var ddl = states.GetState("table_ddls");

            var correctedSql = await instructService.Instruct<string>(query,
                new InstructOptions
                {
                    Provider = "openai",
                    Model = "gpt-4o",
                    AgentId = BuiltInAgentId.SqlDriver,
                    TemplateName = "sql_statement_correctness",
                    Data = new Dictionary<string, object>
                    {
                        { "original_sql", message.Content },
                        { "error_message", ex.Message },
                        { "table_structure", ddl }
                    }
                 });
            message.Content = correctedSql;
        }

        return true;
    }
}
