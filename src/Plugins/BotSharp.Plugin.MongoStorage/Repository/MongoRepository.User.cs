using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using MongoDB.Driver.Linq;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public async Task<User?> GetUserByEmail(string email)
    {
        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Email == email.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public async Task<User?> GetUserByPhone(string phone, string type = UserType.Client, string regionCode = "CN")
    {
        string phoneSecond = string.Empty;
        // if phone number length is less than 4, return null
        if (string.IsNullOrWhiteSpace(phone) || phone?.Length < 4)
        {
            return null;
        }

        if (regionCode == "CN")
        {
            phoneSecond = (phone ?? "").StartsWith("+86") ? (phone ?? "").Replace("+86", "") : ($"+86{phone ?? ""}");
        }
        else
        {
            phoneSecond = (phone ?? "").Substring(regionCode == "US" ? 2 : 3);
        }

        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => (x.Phone == phone || x.Phone == phoneSecond)
        && (x.RegionCode == regionCode || string.IsNullOrWhiteSpace(x.RegionCode))
        && (x.Type == type));
        return user != null ? user.ToUser() : null;
    }

    public async Task<User?> GetUserByPhoneV2(string phone, string source = UserType.Internal, string regionCode = "CN")
    {
        string phoneSecond = string.Empty;
        // if phone number length is less than 4, return null
        if (string.IsNullOrWhiteSpace(phone) || phone?.Length < 4)
        {
            return null;
        }

        if (regionCode == "CN")
        {
            phoneSecond = (phone ?? "").StartsWith("+86") ? (phone ?? "").Replace("+86", "") : ($"+86{phone ?? ""}");
        }
        else
        {
            phoneSecond = (phone ?? "").Substring(regionCode == "US" ? 2 : 3);
        }

        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => (x.Phone == phone || x.Phone == phoneSecond)
            && (x.RegionCode == regionCode || string.IsNullOrWhiteSpace(x.RegionCode))
            && (x.Source == source));
        return user != null ? user.ToUser() : null;
    }

    public async Task<User?> GetAffiliateUserByPhone(string phone)
    {
        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Phone == phone && x.Type == UserType.Affiliate);
        return user != null ? user.ToUser() : null;
    }

    public async Task<User?> GetUserById(string id)
    {
        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
        return user != null ? user.ToUser() : null;
    }

    public async Task<List<User>> GetUserByIds(List<string> ids)
    {
        var users = await _dc.Users.AsQueryable().Where(x => ids.Contains(x.Id) || (x.ExternalId != null && ids.Contains(x.ExternalId))).ToListAsync();
        return users?.Any() == true ? users.Select(x => x.ToUser()).ToList() : [];
    }

    public async Task<List<User>> GetUsersByAffiliateId(string affiliateId)
    {
        var users = await _dc.Users.AsQueryable().Where(x => x.AffiliateId == affiliateId).ToListAsync();
        return users?.Any() == true ? users.Select(x => x.ToUser()).ToList() : [];
    }

    public async Task<User?> GetUserByUserName(string userName)
    {
        var user = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.UserName == userName.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public async Task CreateUser(User user)
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

        await _dc.Users.InsertOneAsync(userCollection);
    }

    public async Task UpdateExistUser(string userId, User user)
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
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserName(string userId, string userName)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update
            .Set(x => x.UserName, userName);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserVerified(string userId)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Verified, true)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserVerificationCode(string userId, string verficationCode)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.VerificationCode, verficationCode)
            .Set(x => x.VerificationCodeExpireAt, DateTime.UtcNow.AddMinutes(5))
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserPassword(string userId, string password)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Password, password)
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.Verified, true);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserEmail(string userId, string email)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Email, email)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserPhone(string userId, string phone, string regionCode)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Phone, phone)
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.RegionCode, regionCode)
            //.Set(x => x.UserName, phone)
            .Set(x => x.FirstName, phone);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUserIsDisable(string userId, bool isDisable)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.IsDisabled, isDisable)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        await _dc.Users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateUsersIsDisable(List<string> userIds, bool isDisable)
    {
        foreach (var userId in userIds)
        {
            await UpdateUserIsDisable(userId, isDisable);
        }
    }

    public async ValueTask<PagedItems<User>> GetUsers(UserFilter filter)
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
        if (!filter.Types.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.Type, filter.Types));
        }
        if (!filter.Sources.IsNullOrEmpty())
        {
            userFilters.Add(userBuilder.In(x => x.Source, filter.Sources));
        }

        // Filter def and sort
        var filterDef = userBuilder.And(userFilters);
        var sortDef = Builders<UserDocument>.Sort.Descending(x => x.CreatedTime);

        // Search
        var docsTask = _dc.Users.FindAsync(filterDef, options: new()
        {
            Sort = sortDef,
            Skip = filter.Offset,
            Limit = filter.Size
        });
        var countTask = _dc.Users.CountDocumentsAsync(filterDef);
        await Task.WhenAll([docsTask, countTask]);

        var docs = (await docsTask).ToList();
        var count = await countTask;

        var users = docs.Select(x => x.ToUser()).ToList();
        return new PagedItems<User>
        {
            Items = users,
            Count = count
        };
    }

    public async Task<List<User>> SearchLoginUsers(User filter, string source = UserSource.Internal)
    {
        List<User> searchResult = [];

        // search by filters
        if (!string.IsNullOrWhiteSpace(filter.Id))
        {
            var curUser = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Source == source && x.Id == filter.Id.ToLower());
            User? user = curUser != null ? curUser.ToUser() : null;
            if (user != null)
            {
                searchResult.Add(user);
            }
        }
        else if (!string.IsNullOrWhiteSpace(filter.Phone) && !string.IsNullOrWhiteSpace(filter.RegionCode))
        {
            string[] regionCodeData = filter.RegionCode.Split('|');
            if (regionCodeData.Length == 2)
            {
                string phoneNoCallingCode = filter.Phone;
                string phoneWithCallingCode = filter.Phone;
                if (!filter.Phone.StartsWith('+'))
                {
                    phoneNoCallingCode = filter.Phone;
                    phoneWithCallingCode = $"{regionCodeData[1]}{filter.Phone}";
                }
                else
                {
                    phoneNoCallingCode = filter.Phone.Replace(regionCodeData[1], "");
                }
                var phoneUsers = await _dc.Users.AsQueryable()
                                          .Where(x => x.Source == source && (x.Phone == phoneNoCallingCode || x.Phone == phoneWithCallingCode) && x.RegionCode == regionCodeData[0])
                                          .ToListAsync();

                if (phoneUsers != null && phoneUsers.Count > 0)
                {
                    foreach (var user in phoneUsers)
                    {
                        if (user != null)
                        {
                            searchResult.Add(user.ToUser());
                        }
                    }
                }

            }
        }
        else if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var curUser = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Source == source && x.Email == filter.Email.ToLower());
            User? user = curUser != null ? curUser.ToUser() : null;
            if (user != null)
            {
                searchResult.Add(user);
            }
        }


        if (searchResult.Count == 0 && !string.IsNullOrWhiteSpace(filter.UserName))
        {
            var curUser = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Source == source && x.UserName == filter.UserName);
            User? user = curUser != null ? curUser.ToUser() : null;
            if (user != null)
            {
                searchResult.Add(user);
            }
        }

        return searchResult;
    }

    public async Task<User?> GetUserDetails(string userId, bool includeAgent = false)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var userDoc = await _dc.Users.AsQueryable().FirstOrDefaultAsync(x => x.Id == userId || x.ExternalId == userId);
        if (userDoc == null) return null;

        var agentActions = new List<UserAgentAction>();
        var user = userDoc.ToUser();
        var userAgents = await _dc.UserAgents.AsQueryable().Where(x => x.UserId == userId).Select(x => new UserAgent
        {
            Id = x.Id,
            UserId = x.UserId,
            AgentId = x.AgentId,
            Actions = x.Actions ?? Enumerable.Empty<string>()
        }).ToListAsync();

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
            var agents = await GetAgents(new AgentFilter { AgentIds = agentIds });

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

    public async Task<bool> UpdateUser(User user, bool updateUserAgents = false)
    {
        if (string.IsNullOrEmpty(user?.Id)) return false;

        var userFilter = Builders<UserDocument>.Filter.Eq(x => x.Id, user.Id);
        var userUpdate = Builders<UserDocument>.Update
            .Set(x => x.Type, user.Type)
            .Set(x => x.Role, user.Role)
            .Set(x => x.Permissions, user.Permissions)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.Users.UpdateOneAsync(userFilter, userUpdate);

        if (updateUserAgents)
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

            var toDelete = await _dc.UserAgents.Find(Builders<UserAgentDocument>.Filter.And(
                    Builders<UserAgentDocument>.Filter.Eq(x => x.UserId, user.Id),
                    Builders<UserAgentDocument>.Filter.Nin(x => x.Id, userAgentDocs.Select(x => x.Id))
                )).ToListAsync();

            await _dc.UserAgents.DeleteManyAsync(Builders<UserAgentDocument>.Filter.In(x => x.Id, toDelete.Select(x => x.Id)));
            foreach (var doc in userAgentDocs)
            {
                var userAgentFilter = Builders<UserAgentDocument>.Filter.Eq(x => x.Id, doc.Id);
                var userAgentUpdate = Builders<UserAgentDocument>.Update
                    .Set(x => x.Id, doc.Id)
                    .Set(x => x.UserId, user.Id)
                    .Set(x => x.AgentId, doc.AgentId)
                    .Set(x => x.Actions, doc.Actions)
                    .Set(x => x.UpdatedTime, DateTime.UtcNow);

                await _dc.UserAgents.UpdateOneAsync(userAgentFilter, userAgentUpdate, _options);
            }
        }

        return true;
    }

    public Task<Dashboard?> GetDashboard(string? userId = null)
    {
        return Task.FromResult<Dashboard?>(null);
    }

    public async Task AddDashboardConversation(string userId, string conversationId)
    {
        var user = await GetUserById(userId);
        if (user == null) return;
        var curDash = user.Dashboard ?? new Dashboard();
        curDash.ConversationList.Add(new DashboardConversation
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId
        });

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Dashboard, curDash)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
    }

    public async Task RemoveDashboardConversation(string userId, string conversationId)
    {
        var user = await GetUserById(userId);
        if (user == null || user.Dashboard == null || user.Dashboard.ConversationList.IsNullOrEmpty()) return;
        var curDash = user.Dashboard;
        var unpinConv = user.Dashboard.ConversationList.FirstOrDefault(
            x => string.Equals(x.ConversationId, conversationId, StringComparison.OrdinalIgnoreCase));
        if (unpinConv == null) return;
        curDash.ConversationList.Remove(unpinConv);

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Dashboard, curDash)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
    }

    public async Task UpdateDashboardConversation(string userId, DashboardConversation dashConv)
    {
        var user = await GetUserById(userId);
        if (user == null || user.Dashboard == null || user.Dashboard.ConversationList.IsNullOrEmpty()) return;
        var curIdx = user.Dashboard.ConversationList.ToList().FindIndex(
            x => string.Equals(x.ConversationId, dashConv.ConversationId, StringComparison.OrdinalIgnoreCase));
        if (curIdx < 0) return;

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Dashboard.ConversationList[curIdx], dashConv)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
    }
}
