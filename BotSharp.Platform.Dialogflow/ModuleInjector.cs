using BotSharp.Core;
using BotSharp.Core.AgentStorage;
using BotSharp.Core.ContextStorage;
using BotSharp.Core.Modules;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BotSharp.Platform.Dialogflow
{
    public class ModuleInjector : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IPlatformBuilder<AgentModel>, DialogflowAi<AgentModel>>();
            services.AddSingleton<DialogflowAi<AgentModel>>();
            AgentStorageServiceRegister.Register<AgentModel>(services);
            PlatformConfigServiceRegister.Register<PlatformSettings>("dialogflowAi", services, config);
            ContextStorageServiceRegister.Register<AIContext>(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

        }
    }
}
