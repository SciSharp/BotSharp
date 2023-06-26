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

namespace BotSharp.Plugin.WeChat
{

    public class WeChatPlugin : IBotSharpAppPlugin
    {
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddMemoryCache();

            services.AddSenparcWeixinServices(config);

            services.AddHostedService<WeChatBackgroundService>();

            services.TryAddSingleton<IMessageQueue>(s => s.GetRequiredService<WeChatBackgroundService>());
        }

        public void Configure(IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
            var register = app.UseSenparcGlobal(env);
            register.UseSenparcWeixin(null, (svc, settings) =>
            {
                svc.RegisterMpAccount(settings, "WeChat");
            }, app.ApplicationServices);

            app.UseMessageHandlerForMp("/WeixinAsync", BotSharpMessageHandler.GenerateMessageHandler, options =>
            {
                options.AccountSettingFunc = context => Senparc.Weixin.Config.SenparcWeixinSetting;
            });

        }
    }
}
