using Bot.Rasa.Agents;
using Bot.Rasa.Consoles;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.RestApi
{
    public class AgentController : EssentialController
    {
        [HttpGet("id")]
        public Agent Get([FromRoute] String id)
        {
            var console = new RasaAi(dc);
            return console.LoadAgent(id);
        }

        [HttpPost]
        public String Create([FromBody] Agent agent)
        {
            var console = new RasaAi(dc);

            dc.DbTran(() => {
                console.SaveAgent(agent);
            });

            return agent.Id;
        }
    }
}
