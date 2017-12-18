using Bot.Rasa.Agents;
using CustomEntityFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Console
{
    public class RasaConsole
    {
        private EntityDbContext dc { get; set; }
        public RasaOptions options { get; set; }

        public RasaConsole(EntityDbContext dc, RasaOptions options)
        {
            this.dc = dc;
            this.options = options;
        }

        public RasaAgent LoadAgent(String agentId)
        {
            return dc.Agent().Find(agentId);
        }

        public String CreateAgent(RasaAgent agent)
        {
            if (dc.Agent().Any(x => x.Name == agent.Name)) return String.Empty;

            dc.Agent().Add(agent);

            return agent.Id;
        }
    }
}
