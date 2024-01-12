using BotSharp.Abstraction.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET;
using Senparc.Weixin;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.Entities;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.Logging;
using BotSharp.Plugin.WeChat.Users;

namespace BotSharp.Plugin.WeChat;

public class WeChatPlugin : IBotSharpAppPlugin
{
    public string Id => "f5e5113b-c1de-4d69-b4b1-9bc6efed7253";
    public string Name => "Tecent Wechat";
    public string Description => "Free messaging and calling app, support voice,photo,video and text messages.";
    public string IconUrl => "https://i.pinimg.com/originals/66/c9/44/66c94415043811725165e59b371a0aa2.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IWeChatAccountUserService,WeChatAccountUserService> ();

        services.AddMemoryCache();

        services.Configure<SenparcWeixinSetting>(config.GetSection("WeChat"));

        if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
        {
            services = services.AddSenparcGlobalServices(config);
        }
        WeChatBackgroundService.AgentId = config["WeChat:AgentId"];

        services.AddSingleton<WeChatBackgroundService>();
        
        services.AddHostedService(s => s.GetRequiredService<WeChatBackgroundService>());

        services.TryAddSingleton<IMessageQueue>(s => s.GetRequiredService<WeChatBackgroundService>());
    }

    public void Configure(IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<WeChatPlugin>>();

        var register = app.UseSenparcGlobal(env);
        register.UseSenparcWeixin(null, (svc, settings) =>
        {
            svc.RegisterMpAccount(settings, "WeChat");
        }, app.ApplicationServices);

        app.UseMessageHandlerForMp("/WeChatAsync", BotSharpMessageHandler.GenerateMessageHandler, options =>
        {
            options.AccountSettingFunc = context => Senparc.Weixin.Config.SenparcWeixinSetting;
            options.EnbleResponseLog = false;
            options.EnableRequestLog = false;
        });

        logger.LogInformation("WeChat Message Handler is running on /WeChatAsync.");

    }
}
