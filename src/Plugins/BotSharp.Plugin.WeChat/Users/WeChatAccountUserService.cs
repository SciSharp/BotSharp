using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Core.Repository;
using BotSharp.Plugin.WeChat.Repository.DbTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.WeChat.Users
{
    public class WeChatAccountUserService : IWeChatAccountUserService
    {
        private readonly IUserService userService;

        public WeChatAccountUserService(IUserService userService)
        {
            this.userService = userService;
        }
        public async Task<User> GetOrCreateWeChatAccountUserAsync(string appId, string openId)
        {
            var user = await userService.CreateUser(new User()
            {
                Email = openId + "@" + appId + ".wechat",
                Password = openId
            });
            return user;
        }
    }
}
