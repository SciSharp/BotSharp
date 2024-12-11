using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users;
using BotSharp.Core.Crontab.Hooks;

namespace BotSharp.Core.Crontab.Functions;

public class ScheduleTaskFn : IFunctionCallback
{
    public string Name => $"{CrontabUtilityHook.PREFIX}schedule_task";
    private readonly IServiceProvider _services;

    public ScheduleTaskFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ScheduleTaskArgs>(message.FunctionArgs);
        if (args.LessThan60Seconds)
        {
            message.Content = "Cron expression should not include seconds.";
            return false;
        }

        var routing = _services.GetRequiredService<IRoutingContext>();
        var user = _services.GetRequiredService<IUserIdentity>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        if (string.IsNullOrEmpty(args.Cron))
        {
            var ret = db.DeleteCrontabItem(routing.ConversationId);
            message.Content = $"Task schedule canceled result: {ret}";
        }
        else
        {
            var crontabItem = new CrontabItem
            {
                Title = args.Title,
                Description = args.Description,
                Cron = args.Cron,
                UserId = user.Id,
                AgentId = routing.EntryAgentId,
                ConversationId = routing.ConversationId,
                Tasks = args.Tasks,
            };

            var ret = db.UpsertCrontabItem(crontabItem);
            message.Content = $"Task scheduled result: {ret}";
        }

        return true;
    }
}
