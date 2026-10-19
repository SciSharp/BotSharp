using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users;
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
    private readonly IUserIdentity _user;

    public SqlDriverController(IServiceProvider services, IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost]
    [Route("/sql-driver/{conversationId}/execute")]
    public async Task<IActionResult> ExecuteSqlQuery([FromRoute] string conversationId, [FromBody] SqlQueryRequest sqlQueryRequest)
    {
        // [Authorize] only requires a logged-in session; it does not verify the
        // caller owns conversationId. Without this check, any authenticated
        // user could execute an arbitrary SQL statement against any configured
        // data source by supplying any conversationId (including one they
        // invent themselves), since SetConversationId performs no ownership
        // check and silently creates the conversation if it doesn't exist.
        // Mirror the same admin-or-owner check ConversationController.GetConversation
        // already applies to reading a conversation's own dialog.
        var userService = _services.GetRequiredService<IUserService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var (isAdmin, currentUser) = await userService.IsAdminUser(_user.Id);
        if (!isAdmin)
        {
            var existing = await conv.GetConversations(new ConversationFilter
            {
                Id = conversationId,
                UserId = currentUser?.Id,
            });
            if (existing.Items?.FirstOrDefault() == null)
            {
                return Forbid();
            }
        }

        var match = Regex.Match(sqlQueryRequest.SqlStatement, @"```sql\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            sqlQueryRequest.SqlStatement = match.Groups[1].Value.Trim();
        }

        var fn = _services.GetRequiredService<IRoutingService>();
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

        if (result)
        {
            return Ok(msg.Content);
        }
        else
        {
            return StatusCode(500, msg.Content);
        }
    }

    [HttpPost]
    [Route("/sql-driver/{conversationId}/result")]
    public async Task<IActionResult> AddQueryExecutionResult([FromRoute] string conversationId, [FromBody] SqlQueryExecutionResult sqlQueryResult)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var (isAdmin, currentUser) = await userService.IsAdminUser(_user.Id);
        if (!isAdmin)
        {
            var existing = await conv.GetConversations(new ConversationFilter
            {
                Id = conversationId,
                UserId = currentUser?.Id,
            });
            if (existing.Items?.FirstOrDefault() == null)
            {
                return Forbid();
            }
        }

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
