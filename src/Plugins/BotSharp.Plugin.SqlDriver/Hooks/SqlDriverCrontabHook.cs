using BotSharp.Core.Crontab.Abstraction;
using BotSharp.Core.Crontab.Models;
using Microsoft.EntityFrameworkCore.Query;

namespace BotSharp.Plugin.SqlDriver.Hooks;

public class SqlDriverCrontabHook : ICrontabHook
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public SqlDriverCrontabHook(IServiceProvider services, ILogger<SqlDriverCrontabHook> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task OnCronTriggered(CrontabItem item)
    {
        if (item.Language != "sql")
        {
            return;
        }

        _logger.LogWarning($"Crontab item triggered: {item.Topic}. Run {item.Language}: {item.Script}");

        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId("73a9ee27-d597-4739-958f-3bd79760ac8e", []);

        var message = new RoleDialogModel(AgentRole.User, $"Run the query")
        {
            FunctionName = "sql_select",
            FunctionArgs = JsonSerializer.Serialize(new SqlStatement
            {
                Statement = item.Script,
                Reason = item.Description
            })
        };
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.Push("ec46f15b-8790-400f-a37f-1e7995b7d6e2");
        await routing.InvokeFunction("sql_select", message);

        item.ConversationId = conv.ConversationId;
        item.AgentId = BuiltInAgentId.SqlDriver;
        item.UserId = "41021346";
        item.ExecutionResult = message.Content;
    }
}
