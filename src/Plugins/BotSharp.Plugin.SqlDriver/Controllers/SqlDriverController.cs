using BotSharp.Abstraction.Models;
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
        conv.SetConversationId(conversationId, [new MessageState("database_type", sqlQueryRequest.DbType)]);
        
        var msg = new RoleDialogModel(AgentRole.User, sqlQueryRequest.SqlStatement)
        {
            CurrentAgentId = sqlQueryRequest.AgentId
        };

        msg.FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
        {
            DbType = sqlQueryRequest.DbType,
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
                storage.Append(conversationId, dialog);
            }
        }

        return Ok(msg.Content);
    }
}
