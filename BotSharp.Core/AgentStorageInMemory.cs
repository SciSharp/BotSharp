using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core
{
    /// <summary>
    /// Save agent instance into memory. 
    /// Caution: Data will be lost once application restarts.
    /// </summary>
    public class AgentStorageInMemory<TExtraData, TEntity> : IAgentStorage<TExtraData, TEntity>
    {
        private static Dictionary<string, StandardAgent<TExtraData, TEntity>> agents;

        public AgentStorageInMemory()
        {
            if (agents == null) agents = new Dictionary<string, StandardAgent<TExtraData, TEntity>>();
        }

        public StandardAgent<TExtraData, TEntity> FetchById(string agentId)
        {
            if (agents.ContainsKey(agentId))
            {
                return agents[agentId];
            }
            else
            {
                return null;
            }
        }

        public StandardAgent<TExtraData, TEntity> FetchByName(string agentName)
        {
            var data = agents.FirstOrDefault(x => x.Value.Name == agentName);

            return data.Value;
        }

        public bool Persist(StandardAgent<TExtraData, TEntity> agent)
        {
            if (String.IsNullOrEmpty(agent.Id))
            {
                agent.Id = Guid.NewGuid().ToString();
                agents[agent.Id] = agent;
            }
            else
            {
                agents[agent.Id] = agent;
            }
            
            return true;
        }

        public List<StandardAgent<TExtraData, TEntity>> Query()
        {
            return agents.Select(x => x.Value).ToList();
        }
    }
}
