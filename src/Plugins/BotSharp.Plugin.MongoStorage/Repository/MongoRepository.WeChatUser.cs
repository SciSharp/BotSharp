using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public WeChatUser? GetWeChatUser(string openId)
    {
        var weChatUser = _dc.WeChatUsers.AsQueryable().FirstOrDefault(x => x.OpenId == openId);
        return weChatUser != null ? weChatUser.ToWeChatUser() : null;
    }

}
