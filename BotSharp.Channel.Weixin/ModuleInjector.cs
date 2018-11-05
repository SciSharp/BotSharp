using BotSharp.Core.Modules;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin.RegisterServices;
using System;

namespace BotSharp.Channel.Weixin
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            // Senparc.CO2NET， Senparc.Weixin
            services.AddSenparcGlobalServices(config)
                .AddSenparcWeixinServices(config);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env/*, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting*/)
        {
            
            //https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore/Startup.cs
            /* IRegisterService register = RegisterService.Start(env, senparcSetting.Value)
                                                        .UseSenparcGlobal();*/
        }
    }
}
