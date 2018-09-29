using BotSharp.Core;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate
{
    /// <summary>
    /// A platform for building conversational interfaces with intelligent agents (chatbots) 
    /// http://spg.ai/projects/articulate
    /// This implementation takes over APIs of Articulate's 7500 port.
    /// </summary>
    public class ArticulateAi<TStorage, TAgent> : 
        PlatformBuilderBase<TStorage, TAgent>, 
        IPlatformBuilder<TStorage, TAgent> 
        where TStorage : IAgentStorage<TAgent>, new()
    {
        public DialogRequestOptions RequestOptions { get; set; }

        public TAgent RecoverAgent(StandardAgent agent)
        {
            if (agent == null) return default(TAgent);

            var agent1 = new AgentModel
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                Language = agent.Language
            };

            return (TAgent)(agent1 as Object);
        }

        public StandardAgent StandardizeAgent(TAgent specificAgent)
        {
            var agent1 = specificAgent as AgentModel;

            var standardAgent = new StandardAgent
            {
                Name = agent1.Name,
                Language = agent1.Language,
                Description = agent1.Description
            };

            return standardAgent;
        }
    }
}
