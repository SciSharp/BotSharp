using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;

namespace BotSharp.Core.AgentStorage
{
    /// <summary>
    /// Save agent instance into memory. 
    /// Caution: Data will be lost once application restarts.
    /// </summary>
    public class AgentStorageInMemory<TAgent> : IAgentStorage<TAgent> where TAgent : AgentBase
    {
        private static Dictionary<string, TAgent> agents;

        public AgentStorageInMemory()
        {
            if (agents == null) agents = new Dictionary<string, TAgent>();
        }

        public async Task<TAgent> FetchById(string agentId)
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

        public async Task<TAgent> FetchByName(string agentName)
        {
            var data = agents.FirstOrDefault(x => x.Value.Name == agentName);

            return data.Value;
        }

        public async Task<bool> Persist(TAgent agent)
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

        public async Task<int> PurgeAllAgents()
        {
            int count = agents.Count;

            agents.Clear();

            return count;
        }

        public async Task<List<TAgent>> Query()
        {
            return agents.Select(x => x.Value).ToList();
        }
    }
}
