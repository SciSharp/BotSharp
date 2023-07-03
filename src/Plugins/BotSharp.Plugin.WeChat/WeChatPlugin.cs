using BotSharp.Abstraction.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET;
using Senparc.Weixin.RegisterServices;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Weixin;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.Entities;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BotSharp.Abstraction.Users;

namespace BotSharp.Plugin.WeChat
{

    public class WeChatPlugin : IBotSharpAppPlugin
    {
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
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
}
