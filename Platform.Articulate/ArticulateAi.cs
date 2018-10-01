using BotSharp.Core;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Tuple<TAgent, DomainModel> GetAgentByDomainId(String domainId)
        {
            var results = GetAllAgents();

            foreach (TAgent agent in results)
            {
                var domain = (agent as AgentModel).Domains.FirstOrDefault(x => x.Id == domainId);

                if (domain != null)
                {
                    return new Tuple<TAgent, DomainModel>(agent, domain);
                }
            }

            return null;
        }

        public Tuple<TAgent, DomainModel, IntentModel> GetAgentByIntentId(String intentId)
        {
            var results = GetAllAgents();

            foreach (TAgent agent in results)
            {
                foreach (DomainModel domain in (agent as AgentModel).Domains)
                {
                    var intent = domain.Intents.FirstOrDefault(x => x.Id == intentId);
                    if (intent != null)
                    {
                        return new Tuple<TAgent, DomainModel, IntentModel>(agent, domain, intent);
                    }
                }
            }

            return null;
        }

        public List<IntentModel> GetReferencedIntentsByEntity(string entityId)
        {
            var intents = new List<IntentModel>();
            var allAgents = GetAllAgents();
            foreach (TAgent agt in allAgents)
            {
                var agent = agt as AgentModel;

                foreach (DomainModel domain in agent.Domains)
                {
                    foreach (IntentModel intent in domain.Intents)
                    {
                        if(intent.Examples.Exists(x => x.Entities.Exists(y => y.EntityId == entityId)))
                        {
                            intents.Add(intent);
                        }
                    }
                }
            }

            return intents;
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
