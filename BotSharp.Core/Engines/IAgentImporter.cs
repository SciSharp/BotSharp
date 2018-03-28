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

        void LoadEntities(Agent agent, string agentDir);

        void LoadIntents(Agent agent, string agentDir);
    }
}
