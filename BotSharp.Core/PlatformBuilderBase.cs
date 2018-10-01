using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TAgent> where TAgent : AgentBase
    {
        public IAgentStorage<TAgent> Storage { get; set; }

        public IConfiguration PlatformConfig { get; set; }


        public List<TAgent> GetAllAgents()
        {
            GetStorage();

            return Storage.Query();
        }


        public TAgent GetAgentById(string agentId)
        {
            GetStorage();

            return Storage.FetchById(agentId);
        }

        public TAgent GetAgentByName(string agentName)
        {
            GetStorage();

            return Storage.FetchByName(agentName);
        }

        public virtual bool SaveAgent(TAgent agent)
        {
            GetStorage();

            // default save agent in FileStorage
            Storage.Persist(agent);

            return true;
        }

        private IAgentStorage<TAgent> GetStorage()
        {
            if (Storage == null)
            {
                string storageName = PlatformConfig.GetValue<String>("AgentStorage");
                switch (storageName)
                {
                    case "AgentStorageInRedis":
                        Storage = Activator.CreateInstance<AgentStorageInRedis<TAgent>>();
                        break;
                    case "AgentStorageInMemory":
                        Storage = Activator.CreateInstance<AgentStorageInMemory<TAgent>>();
                        break;
                }
            }

            return Storage;
        }
    }
}
