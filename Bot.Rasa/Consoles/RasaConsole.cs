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
    public class RasaConsole
    {
        private Database dc { get; set; }
        public static RasaOptions Options { get; set; }
        public static IConfiguration Configuration { get; set; }

        public RasaConsole(Database dc)
        {
            this.dc = dc;
        }

        /// <summary>
        /// Restore a agent instance from json file
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public Agent RestoreAgent(String agentId)
        {
            string json = File.ReadAllText($"{Options.ContentRootPath}\\App_Data\\DbInitializer\\Agents\\{agentId}.json");
            var agent = JsonConvert.DeserializeObject<Agent>(json);

            agent.Id = agentId;
            agent.EntityTypes.ForEach(entityType =>
            {
                entityType.Items = entityType.Values.Select(x => new EntityItem
                {
                    Value = x
                }).ToList();
            });

            agent.Intents.ForEach(intent => {

                intent.Expressions.ForEach(expression =>
                {
                });

            });

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
            return dc.Table<Agent>().Include(x => x.Intents).FirstOrDefault(x => x.Id == agentId);
        }

        public String CreateAgent(Agent agent)
        {
            if (dc.Table<Agent>().Any(x => x.Id == agent.Id)) return String.Empty;

            dc.Table<Agent>().Add(agent);

            return agent.Id;
        }
    }
}
