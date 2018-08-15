using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
using BotSharp.Core.Models;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(IBotPlatform platform)
        {
            _platform = platform;
        }

        [HttpGet]
        public ActionResult<List<Agent>> AllAgents()
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();

            return dc.Table<Agent>().ToList();
        }

        /// <summary>
        /// Restore a agent from a uploaded zip file 
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult Restore([FromRoute] String agentId)
        {
            var botsHeaderFilePath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), $"DbInitializer{Path.DirectorySeparatorChar}Agents{Path.DirectorySeparatorChar}agents.json");
            var agents = JsonConvert.DeserializeObject<List<AgentImportHeader>>(System.IO.File.ReadAllText(botsHeaderFilePath));

            var rasa = new BotSharpAi();
            var agentHeader = agents.First(x => x.Id == agentId);
            rasa.RestoreAgent<AgentImporterInDialogflow>(agentHeader);

            return Ok();
        }

        /// <summary>
        /// Restore a agent from a uploaded zip file 
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public string Train([FromRoute] String agentId)
        {
            _platform.LoadAgent(agentId);
            _platform.Train();

            return "";
        }

        /// <summary>
        /// Dump agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult<Agent> Dump([FromRoute] String agentId)
        {
            var agent = _platform.LoadAgent(agentId);

            return Ok(agent);
        }
    }
}
