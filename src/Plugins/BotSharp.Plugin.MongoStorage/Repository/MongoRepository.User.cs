using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email);
        return user != null ? new User
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role
        } : null;
    }

    public User? GetUserById(string id)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Id == id || x.ExternalId == id);
        return user != null ? new User
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role
        } : null;
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userCollection = new UserDocument
        {
            Id = Guid.NewGuid().ToString(),
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }
}
