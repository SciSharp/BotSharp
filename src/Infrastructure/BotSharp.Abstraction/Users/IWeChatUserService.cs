using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Users;

public interface IWeChatUserService
{
    Task<WeChatUser?> GetWeChatUser(string openId);

    Task<WeChatUser?> CreateWeChatUser(WeChatUser weChatUser);

    Task<WeChatUser?> UpdateWeChatUser(WeChatUser weChatUser);

    Task<WeChatUser> WeChatUserLogin(string code);
}
