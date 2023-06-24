using BotSharp.Abstraction.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Senparc.Weixin.AspNet;
using Senparc.Weixin.RegisterServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.WeChat
{

    public class WeChatPlugin : IBotSharpPlugin
    {
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddMemoryCache();

            services.AddSenparcWeixinServices(config);

            services.AddHostedService<WeChatBackgroundService>();

            services.TryAddSingleton<IMessageQueue>(s => s.GetRequiredService<WeChatBackgroundService>());
        }

        public void ConfigurateApplication(IApplicationBuilder app)
        {
            // TODO: app.UseSenparcWeixin
        }
    }
}
