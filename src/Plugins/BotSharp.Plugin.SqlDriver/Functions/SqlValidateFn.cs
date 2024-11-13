using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Agents.Services;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Instructs;
using BotSharp.Plugin.SqlDriver.Interfaces;
using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlValidateFn : IFunctionCallback
{
    public string Name => "validate_sql";
    public string Indication => "Performing data validate operation.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public SqlValidateFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        string pattern = @"```sql\s*([\s\S]*?)\s*```";
        var sqls = Regex.Match(message.Content, pattern);
        if (!sqls.Success)
        {
            return false;
        }
        var sql = sqls.Groups[1].Value;

        var dbHook = _services.GetRequiredService<ISqlDriverHook>();
        var dbType = dbHook.GetDatabaseType(message);
        var validateSql = dbType.ToLower() switch
        {
            "mysql" => $"explain\r\n{sql}",
            "sqlserver" => $"SET PARSEONLY ON;\r\n{sql}\r\nSET PARSEONLY OFF;",
            "redshift" => $"explain\r\n{sql}",
            _ => throw new NotImplementedException($"Database type {dbType} is not supported.")
        };
        var msgCopy = RoleDialogModel.From(message);
        msgCopy.FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
        {
            SqlStatements = new string[] { validateSql }
        });

        var fn = _services.GetRequiredService<IRoutingService>();
        await fn.InvokeFunction("execute_sql", msgCopy);

        if (msgCopy.Data != null && msgCopy.Data is DbException ex)
        {
            
            var instructService = _services.GetRequiredService<IInstructService>();
            var agentService = _services.GetRequiredService<IAgentService>();
            var states = _services.GetRequiredService<IConversationStateService>();
            
            var agent = await agentService.GetAgent(BuiltInAgentId.SqlDriver);
            var template = agent.Templates.FirstOrDefault(x => x.Name == "sql_statement_correctness")?.Content ?? string.Empty;
            var ddl = states.GetState("table_ddls");

            var correctedSql = await instructService.Instruct<string>(template, BuiltInAgentId.SqlDriver,
                new InstructOptions
                {
                    Provider = agent?.LlmConfig?.Provider ?? "openai",
                    Model = agent?.LlmConfig?.Model ?? "gpt-4o",
                    Message = "Correct SQL Statement",
                    Data = new Dictionary<string, object>
                    {
                        { "original_sql", sql },
                        { "error_message", ex.Message },
                        { "table_structure", ddl }
                    }
                 });
            message.Content = correctedSql;
        }

        return true;
    }
}
