using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;

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
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Phone == phone && x.Type != UserType.Affiliate);
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

    public User? GetUserByAffiliateId(string affiliateId)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.AffiliateId == affiliateId);
        return user != null ? user.ToUser() : null;
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
            AffiliateId = user.AffiliateId,
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
            .Set(x => x.VerificationCode, user.VerificationCode);
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
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserEmail(string userId, string email)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Email, email)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
        _dc.Users.UpdateOne(filter, update);
    }

    public void UpdateUserPhone(string userId, string phone)
    {
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Phone, phone)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
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

    public void AddDashboardConversation(string userId, string conversationId)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == userId || (x.ExternalId != null && x.ExternalId == userId));
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

    public void RemoveDashboardConversation(string userId, string conversationId)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == userId || (x.ExternalId != null && x.ExternalId == userId));
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

    public void UpdateDashboardConversation(string userId, DashboardConversation dashConv)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == userId || (x.ExternalId != null && x.ExternalId == userId));
        if (user == null || user.Dashboard == null || user.Dashboard.ConversationList.IsNullOrEmpty()) return;
        var curIdx = user.Dashboard.ConversationList.ToList().FindIndex(
            x => string.Equals(x.ConversationId, dashConv.ConversationId, StringComparison.OrdinalIgnoreCase));
        if (curIdx < 0) return;

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserDocument>.Update.Set(x => x.Dashboard.ConversationList[curIdx], dashConv)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);
    }
}
