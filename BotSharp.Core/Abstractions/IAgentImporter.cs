using BotSharp.Core.Agents;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface IAgentImporter
    {
        /// <summary>
        /// data dir
        /// </summary>
        string AgentDir { get; set; }

        /// <summary>
        /// Load agent summary
        /// </summary>
        /// <returns></returns>
        Agent LoadAgent(AgentImportHeader agentHeader);

        /// <summary>
        /// Load user customized entity type which defined in dictionary
        /// </summary>
        /// <param name="agent"></param>
        void LoadCustomEntities(Agent agent);

        /// <summary>
        /// Load user customized intents
        /// </summary>
        /// <param name="agent"></param>
        void LoadIntents(Agent agent);

        /// <summary>
        /// Add entities that labeled in intent.UserSays into user customized entity dictionary 
        /// </summary>
        /// <param name="agent"></param>
        void LoadBuildinEntities(Agent agent);

        /// <summary>
        /// generate training data
        /// </summary>
        /// <param name="agent"></param>
        void AssembleTrainData(Agent agent);
    }
}
