using Bot.Rasa.Agents;
using Bot.Rasa.Console;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
            dc.DbTran(() => {



            });

            return agent.Id;
        }
    }
}
