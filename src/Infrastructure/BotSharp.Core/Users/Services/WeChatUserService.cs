using BotSharp.Abstraction.Users.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace BotSharp.Core.Users.Services;

public class WeChatUserService : IWeChatUserService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public WeChatUserService(IServiceProvider services, ILogger<WeChatUserService> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<WeChatUser?> GetWeChatUser(string openId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var weChatUser = db.GetWeChatUser(openId);
        return weChatUser;
    }

    public async Task<WeChatUser> CreateWeChatUser(WeChatUser weChatUser)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.CreateWeChatUser(weChatUser);
    }

    public async Task<WeChatUser?> UpdateWeChatUser(WeChatUser weChatUser)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateWeChatUser(weChatUser);
    }

    #region 微信扫码登录（OAuth 2.0 网页授权）

    public async Task<WeChatUser> WeChatUserLogin(string code)
    {
        // Implement obtaining access_token and openid, and then obtaining user information
        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("Create WeChatUser Error: code is empty, Please check if the code has been obtained correctly!");
            throw new Exception("code is empty");
        }

        var appId = _configuration["WeChatQtoss:AppId"] ?? throw new Exception("AppId is not configured.");
        var appSecret = _configuration["WeChatQtoss:AppSecret"] ?? throw new Exception("AppSecret is not configured.");

        try
        {
            // Step 1: Use the code to obtain the access_token and openid
            var tokenUrl = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={appId}&secret={appSecret}&code={code}&grant_type=authorization_code";

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.GetStringAsync(tokenUrl);

            var tokenData = JObject.Parse(tokenResponse);
            var accessToken = tokenData["access_token"]?.ToString();
            var openId = tokenData["openid"]?.ToString();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(openId))
            {
                _logger.LogError("Create WeChatUser Error: Failed to get access_token or openid");
                throw new Exception("Failed to get access_token or openid");
            }

            // Step 2: Retrieve user information using access_token and openid
            var userInfoUrl = $"https://api.weixin.qq.com/sns/userinfo?access_token={accessToken}&openid={openId}&lang=zh_CN";

            var userInfoResponse = await httpClient.GetStringAsync(userInfoUrl);
            var userInfo = JObject.Parse(userInfoResponse);

            // Retrieve user information
            var nickname = userInfo["nickname"]?.ToString() ?? string.Empty;
            var avatar = userInfo["headimgurl"]?.ToString() ?? string.Empty;
            var country = userInfo["country"]?.ToString() ?? string.Empty;
            var province = userInfo["province"]?.ToString() ?? string.Empty;
            var city = userInfo["city"]?.ToString() ?? string.Empty;
            var sex = userInfo["sex"]?.ToString() ?? "0";
            var privilege = userInfo["privilege"]?.ToString() ?? string.Empty;
            var unionId = userInfo["unionId"]?.ToString() ?? string.Empty;

            // Create or update WeChatUser
            var weChatUser = new WeChatUser
            {
                OpenId = openId,
                NickName = nickname,
                Sex = int.Parse(sex),
                Province = province,
                City = city,
                Country = country,
                Headimgurl = avatar,
                Privilege = privilege.Split(','),
                UnionId = unionId
            };

            return await CreateWeChatUser(weChatUser);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Create WeChatUser Error: {ex.Message}");
            throw new Exception($"Failed to create WeChatUser: {ex.Message}", ex);
        }
        
    }


    #endregion 微信扫码登录（OAuth 2.0 网页授权）
}
