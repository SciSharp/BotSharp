using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
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
            var botsHeaderFilePath = $"{Database.ContentRootPath}App_Data{Path.DirectorySeparatorChar}DbInitializer{Path.DirectorySeparatorChar}Agents{Path.DirectorySeparatorChar}agents.json";
            var agents = JsonConvert.DeserializeObject<List<AgentImportHeader>>(System.IO.File.ReadAllText(botsHeaderFilePath));

            var rasa = new RasaAi();
            var agentHeader = agents.First(x => x.Id == agentId);
            rasa.RestoreAgent<AgentImporterInDialogflow>(agentHeader);

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
