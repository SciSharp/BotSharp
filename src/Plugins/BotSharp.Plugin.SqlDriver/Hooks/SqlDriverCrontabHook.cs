using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.SideCar;
using BotSharp.Core.Crontab.Abstraction;

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
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(item.ConversationId, []);

        _logger.LogWarning($"Crontab item triggered: {item.Title}. {item.Description}");

        foreach (var task in item.Tasks)
        {
            if (task.Language == "text")
            {
                var sidecar = _services.GetService<IConversationSideCar>();
                var response = await sidecar.SendMessage(BuiltInAgentId.AIAssistant, $"{item.ExecutionResult}\r\n{task.Script}", states: new List<MessageState>());
            }
            else if (task.Language == "sql")
            {
                var message = new RoleDialogModel(AgentRole.User, $"Run the query")
                {
                    FunctionName = "sql_select",
                    FunctionArgs = JsonSerializer.Serialize(new SqlStatement
                    {
                        Statement = task.Script,
                        Reason = item.Description
                    })
                };
                var routing = _services.GetRequiredService<IRoutingService>();
                routing.Context.Push(BuiltInAgentId.SqlDriver);
                await routing.InvokeFunction("sql_select", message);

                item.AgentId = BuiltInAgentId.SqlDriver;
                item.UserId = "41021346";
                item.ExecutionResult += message.Content + "\r\n";
            }
        }
    }
}
