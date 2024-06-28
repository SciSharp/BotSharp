using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public User? GetUserById(string id)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
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
            VerificationCode = user.VerificationCode,
            Verified = user.Verified,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
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
}
