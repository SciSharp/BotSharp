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

    public User? GetUserByAffiliateId(string affiliateId)
    {
        return Users.FirstOrDefault(x => x.AffiliateId == affiliateId);
    }

    public User? GetUserByUserName(string userName = null)
    {
        return Users.FirstOrDefault(x => x.UserName == userName.ToLower());
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
