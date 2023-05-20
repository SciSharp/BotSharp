using BotSharp.Core;
using BotSharp.Core.AgentStorage;
using BotSharp.Core.Modules;
using BotSharp.Platform.Rasa.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<RasaAi<AgentModel>>();
            AgentStorageServiceRegister.Register<AgentModel>(services);
            PlatformConfigServiceRegister.Register<PlatformSettings>("rasaAi", services, config);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
