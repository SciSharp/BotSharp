using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using MongoDB.Driver;
using System.Globalization;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public User? GetUserByPhone(string phone)
    {
        string phoneSecond = string.Empty;
        // 如果电话号码长度小于 4，直接返回 null
        if (phone?.Length < 4)
        {
            return null;
        }
        if (phone.Substring(0, 3) != "+86")
        {
            phoneSecond = $"+86{phone}";
        }
        else
        {
            phoneSecond = phone.Replace("+86", "");
        }
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => (x.Phone == phone || x.Phone == phoneSecond) && x.Type != UserType.Affiliate);
        return user != null ? user.ToUser() : null;
    }

    public User? GetAffiliateUserByPhone(string phone)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Phone == phone && x.Type == UserType.Affiliate);
        return user != null ? user.ToUser() : null;
    }

    public User? GetUserById(string id)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
        return user != null ? user.ToUser() : null;
    }

    public List<User> GetUserByIds(List<string> ids)
    {
        var users = _dc.Users.AsQueryable()
            .Where(x => ids.Contains(x.Id) || (x.ExternalId != null && ids.Contains(x.ExternalId))).ToList();
        return users?.Any() == true ? users.Select(x => x.ToUser()).ToList() : new List<User>();
    }

    public List<User> GetUsersByAffiliateId(string affiliateId)
    {
        var users = _dc.Users.AsQueryable()
            .Where(x => x.AffiliateId == affiliateId).ToList();
        return users?.Any() == true ? users.Select(x => x.ToUser()).ToList() : new List<User>();
    }

    public User? GetUserByUserName(string userName)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.UserName == userName.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userCollection = new UserDocument
        {
            Id = user.Id ?? Guid.NewGuid().ToString(),
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            Phone = user.Phone,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role,
            Type = user.Type,
            VerificationCode = user.VerificationCode,
            Verified = user.Verified,
            RegionCode = user.RegionCode,
            AffiliateId = user.AffiliateId,
            EmployeeId = user.EmployeeId,
            IsDisabled = user.IsDisabled,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }

    public void UpdateExistUser(string userId, User user)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update
            .Set(x => x.Email, user.Email)
            .Set(x => x.Phone, user.Phone)
            .Set(x => x.Salt, user.Salt)
            .Set(x => x.Password, user.Password)
            .Set(x => x.VerificationCode, user.VerificationCode)
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.RegionCode, user.RegionCode);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserVerified(string userId)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Verified, true)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserVerificationCode(string userId, string verficationCode)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.VerificationCode, verficationCode)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserPassword(string userId, string password)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Password, password)
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.Verified, true);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserEmail(string userId, string email)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Email, email)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserPhone(string userId, string phone, string regionCode)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Phone, phone)
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.RegionCode, regionCode)
            .Set(x => x.UserName, phone)
            .Set(x => x.FirstName, phone);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserIsDisable(string userId, bool isDisable)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.IsDisabled, isDisable)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUsersIsDisable(List<string> userIds, bool isDisable)
    {
        foreach (var userId in userIds)
        {
            UpdateUserIsDisable(userId, isDisable);
        }
    }

    public PagedItems<User> GetUsers(UserFilter filter)
    {
        if (filter == null)
        {
            filter = UserFilter.Empty();
        }

        var userBuilder = Builders<UserDocument>.Filter;
        var userFilters = new List<FilterDefinition<UserDocument>>() { userBuilder.Empty };

        // Apply filters
        if (!filter.UserIds.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.Id, filter.UserIds));
        }
        if (!filter.UserNames.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.UserName, filter.UserNames));
        }
        if (!filter.ExternalIds.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.ExternalId, filter.ExternalIds));
        }
        if (!filter.Roles.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.Role, filter.Roles));
        }
        if (!filter.Sources.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.Source, filter.Sources));
        }

        // Filter def and sort
        var filterDef = userBuilder.And(userFilters);
        var sortDef = Builders<UserDocument>.Sort.Descending(x => x.CreatedTime);

        // Search
        var userDocs = _dc.Users.Find(filterDef).Sort(sortDef).Skip(filter.Offset).Limit(filter.Size).ToList();
        var count = _dc.Users.CountDocuments(filterDef);

        var users = userDocs.Select(x => x.ToUser()).ToList();
        return new PagedItems<User>
        {
            Items = users,
            Count = (int)count
        };
    }

    public User? GetUserDetails(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var userDoc = _dc.Users.Find(Builders<UserDocument>.Filter.Eq(x => x.Id, userId)).FirstOrDefault();
        if (userDoc == null) return null;

        var user = userDoc.ToUser();

        var userAgents = _dc.UserAgents.AsQueryable().Where(x => x.UserId == userId).Select(x => new UserAgent
        {
            Id = x.Id,
            UserId = x.UserId,
            AgentId = x.AgentId,
            Actions = x.Actions ?? Enumerable.Empty<string>()
        }).ToList();

        var agentActions = new List<UserAgentAction>();
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
                    Actions = item.Actions
                });
            }
        }

        user.AgentActions = agentActions;
        return user;
    }

    public bool UpdateUser(User user, bool isUpdateUserAgents = false)
    {
        if (string.IsNullOrEmpty(user?.Id)) return false;

        var userFilter = Builders<UserDocument>.Filter.Eq(x => x.Id, user.Id);
        var userUpdate = Builders<UserDocument>.Update
            .Set(x => x.Type, user.Type)
            .Set(x => x.Role, user.Role)
            .Set(x => x.Permissions, user.Permissions)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Users.UpdateOne(userFilter, userUpdate);

        if (isUpdateUserAgents)
        {
            var userAgentDocs = user.AgentActions?.Select(x => new UserAgentDocument
            {
                Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                UserId = user.Id,
                AgentId = x.AgentId,
                Actions = x.Actions,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            })?.ToList() ?? [];

            var toDelete = _dc.UserAgents.Find(Builders<UserAgentDocument>.Filter.And(
                    Builders<UserAgentDocument>.Filter.Eq(x => x.UserId, user.Id),
                    Builders<UserAgentDocument>.Filter.Nin(x => x.Id, userAgentDocs.Select(x => x.Id))
                )).ToList();

            _dc.UserAgents.DeleteMany(Builders<UserAgentDocument>.Filter.In(x => x.Id, toDelete.Select(x => x.Id)));
            foreach (var doc in userAgentDocs)
            {
                var userAgentFilter = Builders<UserAgentDocument>.Filter.Eq(x => x.Id, doc.Id);
                var userAgentUpdate = Builders<UserAgentDocument>.Update
                    .Set(x => x.Id, doc.Id)
                    .Set(x => x.UserId, user.Id)
                    .Set(x => x.AgentId, doc.AgentId)
                    .Set(x => x.Actions, doc.Actions)
                    .Set(x => x.UpdatedTime, DateTime.UtcNow);

                _dc.UserAgents.UpdateOne(userAgentFilter, userAgentUpdate, _options);
            }
        }

        return true;
    }
}
