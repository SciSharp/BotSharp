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

    public void UpdateUserVerified(string userId)
    {
        var user = GetUserById(userId);
        user.Verified = true;
        var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, user.Id);
        var path = Path.Combine(dir, USER_FILE);
        File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
    }
}
