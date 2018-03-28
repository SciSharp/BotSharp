using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.RestApi
{
    public class AgentController : EssentialController
    {
        [HttpGet("id")]
        public Agent Get([FromRoute] String id)
        {
            var console = new RasaAi(dc, null);
            return console.LoadAgent();
        }

        /// <summary>
        /// New agent with basic configuration
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        [HttpPost]
        public String Create([FromBody] Agent agent)
        {
            var rasa = new RasaAi(dc, null);

            dc.DbTran(() => {
                rasa.SaveAgent(agent);
            });

            return agent.Id;
        }
    }
}
