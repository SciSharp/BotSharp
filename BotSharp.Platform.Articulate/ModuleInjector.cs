using BotSharp.Core;
using BotSharp.Core.AgentStorage;
using BotSharp.Core.Modules;
using BotSharp.Platform.Articulate.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BotSharp.Platform.Articulate
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ArticulateAi<AgentModel>>();
            AgentStorageServiceRegister.Register<AgentModel>(services);
            PlatformConfigServiceRegister.Register<PlatformSettings>("articulateAi", services, config);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
