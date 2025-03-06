using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Core.Loggers.Services;

public partial class LoggerService
{
    public async Task<PagedItems<InstructionLogModel>> GetInstructionLogs(InstructLogFilter filter)
    {
        if (filter == null)
        {
            filter = InstructLogFilter.Empty();
        }

        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);
        var isAdmin = UserConstant.AdminRoles.Contains(user?.Role);
        if (!isAdmin && user?.Id == null) return new();

        filter.UserIds = isAdmin ? [] : user?.Id != null ? [user.Id] : [];

        var agents = new List<Agent>();
        var users = new List<User>();

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetInstructionLogs(filter);
        var agentIds = logs.Items.Where(x => !string.IsNullOrEmpty(x.AgentId)).Select(x => x.AgentId).ToList();
        var userIds = logs.Items.Where(x => !string.IsNullOrEmpty(x.UserId)).Select(x => x.UserId).ToList();
        agents = db.GetAgents(new AgentFilter
        {
            AgentIds = agentIds,
            Pager = new Pagination { Size = filter.Size }
        });

        if (isAdmin)
        {
            users = db.GetUserByIds(userIds);
        }

        var items = logs.Items.Select(x =>
        {
            x.AgentId = !string.IsNullOrEmpty(x.AgentId) ? agents.FirstOrDefault(a => a.Id == x.AgentId)?.Name : null;

            if (!isAdmin)
            {
                x.UserName = user != null ? $"{user.FirstName} {user.LastName}" : null;
            }
            else
            {
                var found = !string.IsNullOrEmpty(x.UserId) ? users.FirstOrDefault(u => u.Id == x.UserId) : null;
                x.UserName = found != null ? $"{found.FirstName} {found.LastName}" : null;
            }
            return x;
        }).ToList();

        return new PagedItems<InstructionLogModel>
        {
            Items = items,
            Count = logs.Count
        };
    }
}
