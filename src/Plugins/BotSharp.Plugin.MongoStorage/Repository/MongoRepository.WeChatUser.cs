using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public WeChatUser? GetWeChatUser(string openId)
    {
        var weChatUser = _dc.WeChatUsers.AsQueryable().FirstOrDefault(x => x.OpenId == openId);
        return weChatUser != null ? weChatUser.ToWeChatUser() : null;
    }

    public WeChatUser? CreateWeChatUser(WeChatUser weChatUser)
    {
        if (weChatUser == null) return null;

        var weChatUserInfo = new WeChatUserDocument
        {
            Id = weChatUser.Id ?? Guid.NewGuid().ToString(),
            OpenId = weChatUser.OpenId,
            SessionKey = weChatUser.SessionKey,
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

    
}
