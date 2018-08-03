using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi
{
    /// <summary>
    /// Agent
    /// </summary>
    [Route("v1/[controller]/[action]")]
    public class AgentController : ControllerBase
    {
        /// <summary>
        /// Restore a agent from a uploaded zip file 
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult Restore([FromRoute] String agentId)
        {
            var botsHeaderFilePath = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), $"DbInitializer{Path.DirectorySeparatorChar}Agents{Path.DirectorySeparatorChar}agents.json");
            var agents = JsonConvert.DeserializeObject<List<AgentImportHeader>>(System.IO.File.ReadAllText(botsHeaderFilePath));

            var rasa = new BotSharpAi();
            var agentHeader = agents.First(x => x.Id == agentId);
            rasa.RestoreAgent<AgentImporterInSebis>(agentHeader);

            return Ok();
        }

        /// <summary>
        /// Dump agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult<Agent> Dump([FromRoute] String agentId)
        {
            var rasa = new RasaAi();
            var agent = rasa.LoadAgent(agentId);

            return Ok(agent);
        }
    }
}
