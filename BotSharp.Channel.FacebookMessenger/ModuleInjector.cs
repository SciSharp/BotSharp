using BotSharp.Core.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Channel.FacebookMessenger
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env/*, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting*/)
        {
            
        }
    }
}
