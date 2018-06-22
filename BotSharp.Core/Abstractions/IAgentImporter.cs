using BotSharp.Core.Agents;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface IAgentImporter
    {
        /// <summary>
        /// Load agent summary
        /// </summary>
        /// <param name="agentId">agent guid or name</param>
        /// <param name="agentDir"></param>
        /// <returns></returns>
        Agent LoadAgent(string agentId, string agentDir);

        /// <summary>
        /// Load user customized entity type
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="agentDir"></param>
        void LoadCustomEntities(Agent agent, string agentDir);

        /// <summary>
        /// Load user customized intents
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="agentDir"></param>
        void LoadIntents(Agent agent, string agentDir);

        /// <summary>
        /// add to user customized entities
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="agentDir"></param>
        void LoadBuildinEntities(Agent agent, string agentDir);
    }
}
