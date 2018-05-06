using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Rasa nlu 0.11.x
    /// </summary>
    public class RasaAi
    {
        public Database dc { get; set; }
        public AIConfiguration AiConfig { get; set; }
        public static IConfiguration Configuration { get; set; }

        public Agent agent { get; set; }

        public RasaAi(Database dc)
        {
            this.dc = dc;
        }

        public RasaAi(Database dc, AIConfiguration aiConfig)
        {
            this.dc = dc;

            AiConfig = aiConfig;
            agent = LoadAgent();
            aiConfig.DevMode = agent.DeveloperAccessToken == aiConfig.ClientAccessToken;
        }

        /// <summary>
        /// Restore a agent instance from backup json files
        /// </summary>
        /// <param name="importor"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public Agent RestoreAgent(IAgentImporter importer, String agentId, string dataDir)
        {
            // Load agent summary
            agent = importer.LoadAgent(agentId, dataDir);

            // Load agent entities
            importer.LoadEntities(agent, dataDir);

            // Load agent intents
            importer.LoadIntents(agent, dataDir);

            return agent;
        }

        /// <summary>
        /// Dump agent train data to json file
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public bool DumpAgent(String agentId)
        {
            return true;
        }

        public Agent LoadAgent()
        {
            return dc.Table<Agent>()
                .Include(x => x.Intents).ThenInclude(x => x.Contexts)
                .FirstOrDefault(x => x.ClientAccessToken == AiConfig.ClientAccessToken || x.DeveloperAccessToken == AiConfig.ClientAccessToken);
        }

        public String SaveAgent(Agent agent)
        {
            var existedAgent = dc.Table<Agent>().FirstOrDefault(x => x.Id == agent.Id || x.Name == agent.Name);
            if (existedAgent == null)
            {
                dc.Table<Agent>().Add(agent);
                return agent.Id;
            }
            else
            {
                agent.Id = existedAgent.Id;
                return existedAgent.Id;
            }
        }
    }
}
