using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public WeChatUser? GetWeChatUser(string openId)
    {
        var weChatUser = _dc.WeChatUsers.AsQueryable().FirstOrDefault(x => x.OpenId == openId);
        return weChatUser != null ? weChatUser.ToWeChatUser() : null;
    }

    public WeChatUser CreateWeChatUser(WeChatUser weChatUser)
    {
        var weChatUserInfo = new WeChatUserDocument
        {
            Id = weChatUser.Id ?? Guid.NewGuid().ToString(),
            OpenId = weChatUser.OpenId,
            UnionId = weChatUser.UnionId,
            Sex = weChatUser.Sex,
            Province = weChatUser.Province,
            City = weChatUser.City,
            NickName = weChatUser.NickName,
            Headimgurl = weChatUser.Headimgurl,
            PhoneNumber = weChatUser.PhoneNumber,
            Country = weChatUser.Country,
            Privilege = weChatUser.Privilege,
            CreatedAt = DateTime.UtcNow,
        };

        _dc.WeChatUsers.InsertOne(weChatUserInfo);

        return weChatUserInfo.ToWeChatUser();
    }

    public WeChatUser? UpdateWeChatUser(WeChatUser weChatUser)
    {
        if (weChatUser == null) return null;

        var filter = Builders<WeChatUserDocument>.Filter.Eq(x => x.Id, weChatUser.Id);
        var update = Builders<WeChatUserDocument>.Update
            .Set(x => x.OpenId, weChatUser.OpenId)
            .Set(x => x.UnionId, weChatUser.UnionId)
            .Set(x => x.Sex, weChatUser.Sex)
            .Set(x => x.Province, weChatUser.Province)
            .Set(x => x.City, weChatUser.City)
            .Set(x => x.NickName, weChatUser.NickName)
            .Set(x => x.Headimgurl, weChatUser.Headimgurl)
            .Set(x => x.PhoneNumber, weChatUser.PhoneNumber)
            .Set(x => x.Country, weChatUser.Country)
            .Set(x => x.Privilege, weChatUser.Privilege)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        _dc.WeChatUsers.UpdateOne(filter, update);

        return weChatUser;
    }
}
