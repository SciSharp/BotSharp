using BotSharp.Core;
using BotSharp.Core.AgentStorage;
using BotSharp.Core.ContextStorage;
using BotSharp.Core.Modules;
using BotSharp.Platform.Abstractions;
using BotSharp.Platform.OwnThink.Models;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BotSharp.Platform.OwnThink
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IPlatformBuilder<AgentModel>, OwnThinkAi<AgentModel>>();
            services.AddSingleton<OwnThinkAi<AgentModel>>();
            AgentStorageServiceRegister.Register<AgentModel>(services);
            PlatformConfigServiceRegister.Register<PlatformSettings>("ownThinkAi", services, config);
            ContextStorageServiceRegister.Register<AIContext>(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

        }
    }
}
