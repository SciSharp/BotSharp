using BotSharp.Plugin.SqlDriver.Controllers.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static Dapper.SqlMapper;

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
        var msg = new RoleDialogModel(AgentRole.User, sqlQueryRequest.SqlStatement);
        msg.FunctionArgs = JsonSerializer.Serialize(new ExecuteQueryArgs
        {
            SqlStatements = [sqlQueryRequest.SqlStatement],
            FormattingResult = sqlQueryRequest.FormattingResult
        });
        var result = await fn.InvokeFunction("execute_sql", msg);
        return Ok(msg.Content);
    }
}
