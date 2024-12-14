using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public User? GetUserByEmail(string email)
    {
        return Users.FirstOrDefault(x => x.Email == email.ToLower());
    }

    public User? GetUserByPhone(string phone, string? type = UserType.Client, string regionCode = "CN")
    {
        var query = Users.Where(x => x.Phone == phone);

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(x => x.Type == type);
        }

        if (!string.IsNullOrEmpty(regionCode))
        {
            query = query.Where(x => x.RegionCode == regionCode);
        }

        return query.FirstOrDefault();
    }

    public User? GetAffiliateUserByPhone(string phone)
    {
        return Users.FirstOrDefault(x => x.Phone == phone && x.Type == UserType.Affiliate);
    }

    public User? GetUserById(string id = null)
    {
        return Users.FirstOrDefault(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
    }

    public List<User> GetUserByIds(List<string> ids)
    {
        return Users.Where(x => ids.Contains(x.Id) || (x.ExternalId != null && ids.Contains(x.ExternalId)))?.ToList() ?? new List<User>();
    }

    public List<User> GetUsersByAffiliateId(string affiliateId)
    {
        return Users.Where(x => x.AffiliateId == affiliateId).ToList();
    }

    public User? GetUserByUserName(string userName = null, string regionCode = "CN")
    {
        return Users.FirstOrDefault(x => x.UserName == userName.ToLower() && x.RegionCode == regionCode.ToLower());
    }

    public Dashboard? GetDashboard(string id = null)
    {
        return Dashboards.FirstOrDefault();
    }

    public void CreateUser(User user)
    {
        var userId = Guid.NewGuid().ToString();
        user.Id = userId;
        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, userId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var path = Path.Combine(dir, USER_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
    }

    public void UpdateExistUser(string userId, User user)
    {
        user.Id = userId;
        CreateUser(user);
    }

    public void UpdateUserVerified(string userId)
    {
        var user = GetUserById(userId);
        user.Verified = true;
        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, user.Id);
        var path = Path.Combine(dir, USER_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
    }

    public PagedItems<User> GetUsers(UserFilter filter)
    {
        if (filter == null)
        {
            filter = UserFilter.Empty();
        }

        var users = Users;

        // Apply filters
        if (!filter.UserIds.IsNullOrEmpty())
        {
            users = users.Where(x => filter.UserIds.Contains(x.Id));
        }
        if (!filter.UserNames.IsNullOrEmpty())
        {
            users = users.Where(x => filter.UserNames.Contains(x.UserName));
        }
        if (!filter.ExternalIds.IsNullOrEmpty())
        {
            users = users.Where(x => filter.ExternalIds.Contains(x.ExternalId));
        }
        if (!filter.Roles.IsNullOrEmpty())
        {
            users = users.Where(x => filter.Roles.Contains(x.Role));
        }
        if (!filter.Types.IsNullOrEmpty())
        {
            users = users.Where(x => filter.Types.Contains(x.Type));
        }
        if (!filter.Sources.IsNullOrEmpty())
        {
            users = users.Where(x => filter.Sources.Contains(x.Source));
        }

        return new PagedItems<User>
        {
            Items = users.OrderByDescending(x => x.CreatedTime).Skip(filter.Offset).Take(filter.Size),
            Count = users.Count()
        };
    }

    public User? GetUserDetails(string userId, bool includeAgent = false)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var user = Users.FirstOrDefault(x => x.Id == userId || x.ExternalId == userId);
        if (user == null) return null;

        var agentActions = new List<UserAgentAction>();
        var userAgents = UserAgents?.Where(x => x.UserId == userId)?.ToList() ?? [];

        if (!includeAgent)
        {
            agentActions = userAgents.Select(x => new UserAgentAction
            {
                Id = x.Id,
                AgentId = x.AgentId,
                Actions = x.Actions
            }).ToList();
            user.AgentActions = agentActions;
            return user;
        }

        var agentIds = userAgents.Select(x => x.AgentId)?.Distinct().ToList();
        if (!agentIds.IsNullOrEmpty())
        {
            var agents = GetAgents(new AgentFilter { AgentIds = agentIds });

            foreach (var item in userAgents)
            {
                var found = agents.FirstOrDefault(x => x.Id == item.AgentId);
                if (found == null) continue;

                agentActions.Add(new UserAgentAction
                {
                    Id = item.Id,
                    AgentId = found.Id,
                    Agent = found,
                    Actions = item.Actions ?? []
                });
            }
        }

        user.AgentActions = agentActions;
        return user;
    }

    public bool UpdateUser(User user, bool updateUserAgents = false)
    {
        if (string.IsNullOrEmpty(user?.Id)) return false;

        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, user.Id);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var userFile = Path.Combine(dir, USER_FILE);
        user.UpdatedTime = DateTime.UtcNow;
        File.WriteAllText(userFile, JsonSerializer.Serialize(user, _options));

        if (updateUserAgents)
        {
            var userAgents = user.AgentActions?.Select(x => new UserAgent
            {
                Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                UserId = user.Id,
                AgentId = x.AgentId,
                Actions = x.Actions ?? [],
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            })?.ToList() ?? [];

            var userAgentFile = Path.Combine(dir, USER_AGENT_FILE);
            File.WriteAllText(userAgentFile, JsonSerializer.Serialize(userAgents, _options));
            _userAgents = [];
        }

        _users = [];
        return true;
    }

    public void AddDashboardConversation(string userId, string conversationId)
    {
        var user = GetUserById(userId);
        if (user == null) return;

        // one user only has one dashboard currently
        var dash = Dashboards.FirstOrDefault();
        dash ??= new();
        var existingConv = dash.ConversationList.FirstOrDefault(x => string.Equals(x.ConversationId, conversationId, StringComparison.OrdinalIgnoreCase));
        if (existingConv != null) return;

        var dashconv = new DashboardConversation
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId
        };

        dash.ConversationList.Add(dashconv);

        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, userId);
        var path = Path.Combine(dir, DASHBOARD_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(dash, _options));
    }

    public void RemoveDashboardConversation(string userId, string conversationId)
    {
        var user = GetUserById(userId);
        if (user == null) return;

        // one user only has one dashboard currently
        var dash = Dashboards.FirstOrDefault();
        if (dash == null) return;

        var dashconv = dash.ConversationList.FirstOrDefault(
            c => string.Equals(c.ConversationId, conversationId, StringComparison.OrdinalIgnoreCase));
        if (dashconv == null) return;

        dash.ConversationList.Remove(dashconv);

        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, userId);
        var path = Path.Combine(dir, DASHBOARD_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(dash, _options));
    }

    public void UpdateDashboardConversation(string userId, DashboardConversation dashConv)
    {
        var user = GetUserById(userId);
        if (user == null) return;

        // one user only has one dashboard currently
        var dash = Dashboards.FirstOrDefault();
        if (dash == null) return;

        var curIdx = dash.ConversationList.ToList().FindIndex(
            x => string.Equals(x.ConversationId, dashConv.ConversationId, StringComparison.OrdinalIgnoreCase));
        if (curIdx < 0) return;

        dash.ConversationList[curIdx] = dashConv;

        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, userId);
        var path = Path.Combine(dir, DASHBOARD_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(dash, _options));
    }
}
