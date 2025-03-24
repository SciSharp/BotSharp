using BotSharp.Abstraction.Loggers.Models;
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
        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        if (!isAdmin && user?.Id == null) return new();

        filter.UserIds = !isAdmin && user?.Id != null ? [user.Id] : null;

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
            x.AgentName = !string.IsNullOrEmpty(x.AgentId) ? agents.FirstOrDefault(a => a.Id == x.AgentId)?.Name : null;

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

    public async Task<List<string>> GetInstructionLogSearchKeys(InstructLogKeysFilter filter)
    {
        if (filter == null)
        {
            filter = InstructLogKeysFilter.Empty();
        }

        var keys = new List<string>();
        if (!filter.PreLoad && string.IsNullOrWhiteSpace(filter.Query))
        {
            return keys;
        }

        var userService = _services.GetRequiredService<IUserService>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        filter.UserIds = !isAdmin && user?.Id != null ? [user.Id] : null;
        keys = db.GetInstructionLogSearchKeys(filter);
        keys = filter.PreLoad ? keys : keys.Where(x => x.Contains(filter.Query ?? string.Empty, StringComparison.OrdinalIgnoreCase)).ToList();
        return keys.OrderBy(x => x).Take(filter.KeyLimit).ToList();
    }
}
