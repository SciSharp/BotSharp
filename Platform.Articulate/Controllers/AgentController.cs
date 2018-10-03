using BotSharp.Core;
using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.Articulate;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IBotPlatform _platform;
        private ArticulateAi<AgentModel> builder;

        /// <summary>
        /// Initialize agent controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(IBotPlatform platform, IConfiguration configuration)
        {
            _platform = platform;
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpPost]
        public AgentModel PostAgent()
        {
            AgentModel agent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                agent = JsonConvert.DeserializeObject<AgentModel>(body);
            }

            // convert to standard Agent structure
            agent.Id = new Random().Next(Int32.MaxValue).ToString();
            agent.Name = agent.AgentName;
            builder.SaveAgent(agent);

            return agent;
        }

        [HttpGet]
        public List<AgentModel> GetAgent()
        {
            var results = builder.GetAllAgents();
            var agents = results.ToList();

            return agents;
        }

        [HttpGet("{agentId}")]
        public AgentModel GetAgentById([FromRoute] string agentId)
        {
            var agent = builder.GetAgentById(agentId);
            
            return agent;
        }

        [HttpGet("name/{agentName}")]
        public AgentModel GetAgentByName([FromRoute] string agentName)
        {
            var agent = builder.GetAgentByName(agentName);
            
            return agent;
        }
    }
#endif
}
