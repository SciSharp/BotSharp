using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Abstraction
{
    /// <summary>
    /// Agent could be persisted in any kind of storage.
    /// Example: local file storage, cloud storage, relational database, key-value database or memory
    /// </summary>
    public interface IAgentStorage<TExtraData, TEntity>
    {
        /// <summary>
        /// Save agent instance
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool Persist(StandardAgent<TExtraData, TEntity> agent);

        /// <summary>
        /// Get agent by id
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        StandardAgent<TExtraData, TEntity> FetchById(string agentId);

        /// <summary>
        /// Get agent by name
        /// </summary>
        /// <param name="agentName"></param>
        /// <returns></returns>
        StandardAgent<TExtraData, TEntity> FetchByName(string agentName);

        /// <summary>
        /// Query agents
        /// </summary>
        /// <returns></returns>
        List<StandardAgent<TExtraData, TEntity>> Query();
    }
}
