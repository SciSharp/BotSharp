using Bot.Rasa.Agents;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Consoles
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
