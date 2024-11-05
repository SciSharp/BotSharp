using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using System;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public User? GetUserByEmail(string email)
    {
        return Users.FirstOrDefault(x => x.Email == email.ToLower());
    }

    public User? GetUserByPhone(string phone)
    {
        return Users.FirstOrDefault(x => x.Phone == phone);
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

    public User? GetUserByUserName(string userName = null)
    {
        return Users.FirstOrDefault(x => x.UserName == userName.ToLower());
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
        if (!filter.Sources.IsNullOrEmpty())
        {
            users = users.Where(x => filter.Sources.Contains(x.Source));
        }

        // Get user agents
        var userIds = users.Select(x => x.Id).ToList();
        var userAgents = UserAgents.Where(x => userIds.Contains(x.UserId)).ToList();
        var agentIds = userAgents?.Select(x => x.AgentId)?.Distinct()?.ToList() ?? [];

        if (!agentIds.IsNullOrEmpty())
        {
            var agents = GetAgents(new AgentFilter { AgentIds = agentIds });
            foreach (var item in userAgents)
            {
                item.Agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
            }

            foreach (var user in users)
            {
                var found = userAgents.Where(x => x.UserId == user.Id).ToList();
                if (found.IsNullOrEmpty()) continue;

                user.AgentActions = found.Select(x => new UserAgentAction
                {
                    Id = x.Id,
                    AgentId = x.AgentId,
                    Agent = x.Agent,
                    Actions = x.Actions
                });
            }
        }

        return new PagedItems<User>
        {
            Items = users.OrderByDescending(x => x.CreatedTime).Skip(filter.Offset).Take(filter.Size),
            Count = users.Count()
        };
    }

    public bool UpdateUser(User user, bool isUpdateUserAgents = false)
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

        if (isUpdateUserAgents)
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
        }

        _users = [];
        _userAgents = [];
        return true;
    }
}
