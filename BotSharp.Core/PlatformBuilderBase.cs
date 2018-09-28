using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TStorage, TExtraData, TEntity> 
        where TStorage : IAgentStorage<TExtraData, TEntity>, new ()
    {
        protected static TStorage storage;

        public PlatformBuilderBase()
        {
            if (storage == null) storage = new TStorage();
        }

        public List<StandardAgent<TExtraData, TEntity>> GetAllAgents()
        {
            return storage.Query();
        }


        public StandardAgent<TExtraData, TEntity> GetAgentById(string agentId)
        {
            return storage.FetchById(agentId);
        }

        public StandardAgent<TExtraData, TEntity> GetAgentByName(string agentName)
        {
            return storage.FetchByName(agentName);
        }

        public virtual bool SaveAgent(StandardAgent<TExtraData, TEntity> agent)
        {
            // default save agent in FileStorage
            storage.Persist(agent);

            return true;
        }
    }
}
