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

        var routing = _services.GetRequiredService<IRoutingContext>();
        var user = _services.GetRequiredService<IUserIdentity>();
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

        var db = _services.GetRequiredService<IBotSharpRepository>();
        // var ret = db.InsertCrontabItem(crontabItem);

        return true;
    }
}
