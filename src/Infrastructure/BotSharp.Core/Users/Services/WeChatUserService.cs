using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Core.Users.Services;

public class WeChatUserService : IWeChatUserService
{
    private readonly IServiceProvider _services;
    public WeChatUserService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<WeChatUser?> GetWeChatUser(string openId)
    {
        var db =  _services.GetRequiredService<IBotSharpRepository>();
        var weChatUser = db.GetWeChatUser(openId);
        return weChatUser;
    }

    public async Task<WeChatUser?> CreateWeChatUser(WeChatUser weChatUser)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.CreateWeChatUser(weChatUser);
    }

    public async Task<WeChatUser?> UpdateWeChatUser(WeChatUser weChatUser)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateWeChatUser(weChatUser);
    }
}
