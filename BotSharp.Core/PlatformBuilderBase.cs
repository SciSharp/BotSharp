using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TStorage, TAgent> 
        where TStorage : IAgentStorage<TAgent>, new ()
    {
        protected static TStorage storage;

        public PlatformBuilderBase()
        {
            if (storage == null) storage = new TStorage();
        }

        public List<TAgent> GetAllAgents()
        {
            return storage.Query();
        }


        public TAgent GetAgentById(string agentId)
        {
            return storage.FetchById(agentId);
        }

        public TAgent GetAgentByName(string agentName)
        {
            return storage.FetchByName(agentName);
        }

        public virtual bool SaveAgent(TAgent agent)
        {
            // default save agent in FileStorage
            storage.Persist(agent);

            return true;
        }
    }
}
