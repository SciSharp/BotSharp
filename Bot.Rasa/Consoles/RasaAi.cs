using Bot.Rasa.Agents;
using Bot.Rasa.Entities;
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

namespace Bot.Rasa.Consoles
{
    public class RasaAi
    {
        private Database dc { get; set; }
        public static RasaOptions Options { get; set; }
        public static IConfiguration Configuration { get; set; }

        public Agent agent { get; set; }

        public String SessionId { get; set; }

        public RasaAi(Database dc)
        {
            this.dc = dc;
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Restore a agent instance from backup json files
        /// </summary>
        /// <param name="importor"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public Agent RestoreAgent(IAgentImporter importer, String agentId)
        {
            string dataDir = $"{Options.ContentRootPath}\\App_Data\\DbInitializer\\Agents\\";

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

        public Agent LoadAgent(String agentId)
        {
            return dc.Table<Agent>()
                .Include(x => x.Intents).ThenInclude(x => x.Contexts)
                .FirstOrDefault(x => x.Id == agentId);
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
