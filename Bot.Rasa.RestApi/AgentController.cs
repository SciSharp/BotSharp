using Bot.Rasa.Agents;
using Bot.Rasa.Console;
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
            var console = new RasaConsole(dc);
            return console.LoadAgent(id);
        }

        [HttpPost]
        public String Create([FromBody] Agent agent)
        {
            var console = new RasaConsole(dc);

            dc.DbTran(() => {
                console.CreateAgent(agent);
            });

            return agent.Id;
        }
    }
}
