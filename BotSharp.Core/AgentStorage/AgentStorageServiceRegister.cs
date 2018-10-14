using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.AgentStorage
{
    public class AgentStorageServiceRegister
    {
        public static void Register<TAgent>(IServiceCollection services) 
            where TAgent : AgentBase
        {
            services.AddSingleton<IAgentStorageFactory<TAgent>, AgentStorageFactory<TAgent>>();

            services.AddSingleton<AgentStorageInMemory<TAgent>>();
            services.AddSingleton<AgentStorageInRedis<TAgent>>();

            services.AddSingleton(factory =>
            {
                Func<string, IAgentStorage<TAgent>> accesor = key =>
                {
                    if (key.Equals("AgentStorageInRedis"))
                    {
                        return factory.GetService<AgentStorageInRedis<TAgent>>();
                    }
                    else if (key.Equals("AgentStorageInMemory"))
                    {
                        return factory.GetService<AgentStorageInMemory<TAgent>>();
                    }
                    else
                    {
                        throw new ArgumentException($"Not Support key : {key}");
                    }
                };

                return accesor;
            });
        }
    }
}
