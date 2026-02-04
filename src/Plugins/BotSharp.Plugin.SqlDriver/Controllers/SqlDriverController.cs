using BotSharp.Abstraction.Models;
using BotSharp.Plugin.SqlDriver.Constants;
using BotSharp.Plugin.SqlDriver.Controllers.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.SqlDriver.Controllers;

[Authorize]
[ApiController]
public class SqlDriverController : ControllerBase
{
    private readonly IServiceProvider _services;

    public SqlDriverController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost]
    [Route("/sql-driver/{conversationId}/execute")]
    public async Task<IActionResult> ExecuteSqlQuery([FromRoute] string conversationId, [FromBody] SqlQueryRequest sqlQueryRequest)
    {
        var match = Regex.Match(sqlQueryRequest.SqlStatement, @"```sql\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            sqlQueryRequest.SqlStatement = match.Groups[1].Value.Trim();
        }

        var fn = _services.GetRequiredService<IRoutingService>();
        var conv = _services.GetRequiredService<IConversationService>();
        await conv.SetConversationId(conversationId, 
            [
                new MessageState(StateKeys.DBType, sqlQueryRequest.DbType),
                new MessageState(StateKeys.DataSource, sqlQueryRequest.DataSource),
            ]);
        
        var msg = new RoleDialogModel(AgentRole.User, sqlQueryRequest.SqlStatement)
        {
            CurrentAgentId = sqlQueryRequest.AgentId
        };

        msg.FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
        {
            DbType = sqlQueryRequest.DbType,
            DataSource = sqlQueryRequest.DataSource,
            SqlStatements = [sqlQueryRequest.SqlStatement],
            ResultFormat = sqlQueryRequest.ResultFormat
        });
        var result = await fn.InvokeFunction("execute_sql", msg);

        // insert sql result to conversation dialogs as function response
        if (!sqlQueryRequest.IsEphemeral)
        {
            var storage = _services.GetService<IConversationStorage>();
            if (storage != null)
            {
                var dialog = new RoleDialogModel(AgentRole.Assistant, msg.Content)
                {
                    CurrentAgentId = msg.CurrentAgentId,
                    MessageId = msg.MessageId,
                    CreatedAt = DateTime.UtcNow
                };
                await storage.Append(conversationId, dialog);
            }
        }

        return Ok(msg.Content);
    }

    [HttpPost]
    [Route("/sql-driver/{conversationId}/result")]
    public async Task<IActionResult> AddQueryExecutionResult([FromRoute] string conversationId, [FromBody] SqlQueryExecutionResult sqlQueryResult)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        await conv.SetConversationId(conversationId, []);

        var storage = _services.GetRequiredService<IConversationStorage>();
        var dialog = new RoleDialogModel(AgentRole.Assistant, sqlQueryResult.Results)
        {
            CurrentAgentId = sqlQueryResult.AgentId,
            CreatedAt = DateTime.UtcNow,
            MessageId = sqlQueryResult.SqlUniqueId,
            MessageLabel = "sql_query_result"
        };
        await storage.Append(conversationId, dialog);

        return Ok(dialog);
    }

    [HttpGet]
    [Route("/sql-driver/connections")]
    public IActionResult GetConnectionSettings()
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();

        var connections = settings.Connections.Select(x => new DataSourceSetting
        {
            DbType = x.DbType,
            Name = x.Name,
            ConnectionString = "**********"
        }).ToArray();

        return Ok(connections);
    }
}
