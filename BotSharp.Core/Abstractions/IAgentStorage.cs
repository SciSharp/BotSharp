using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    /// <summary>
    /// Agent could be persisted in any kind of storage.
    /// Example: local file storage, cloud storage, relational database, key-value database or memory
    /// </summary>
    public interface IAgentStorage<TAgent>
    {
        /// <summary>
        /// Save agent instance
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        Task<bool> Persist(TAgent agent);

        /// <summary>
        /// Get agent by id
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        Task<TAgent> FetchById(string agentId);

        /// <summary>
        /// Get agent by name
        /// </summary>
        /// <param name="agentName"></param>
        /// <returns></returns>
        Task<TAgent> FetchByName(string agentName);

        /// <summary>
        /// Query agents
        /// </summary>
        /// <returns></returns>
        Task<List<TAgent>> Query();

        /// <summary>
        /// Delete agents
        /// </summary>
        /// <returns></returns>
        Task<int> PurgeAllAgents();
    }
}
