using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.WeChat.Users
{
    public interface IWeChatAccountUserService
    {
        Task<User> GetOrCreateWeChatAccountUserAsync(string appId, string openId);
    }
}
