using BotSharp.Core;
using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IBotPlatform _platform;
        private ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel> builder;

        /// <summary>
        /// Initialize agent controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(IBotPlatform platform)
        {
            _platform = platform;
            builder = new ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel>();
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
            var builder = new ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel>();
            agent.Id = Guid.NewGuid().ToString();
            agent.Name = agent.AgentName;
            builder.SaveAgent(agent);

            return agent;
        }

        [HttpGet]
        public List<AgentModel> GetAgent()
        {
            var results = builder.GetAllAgents();
            var agents = results.Select(x => x as AgentModel).ToList();

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
            AgentModel agent = null;

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => Regex.IsMatch(x, @"agent-\d+\.json")).ToList();

            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var agentTmp = JsonConvert.DeserializeObject<AgentModel>(json);

                if(agentTmp.Name == agentName)
                {
                    agent = agentTmp;
                }
            }

            return agent;
        }
    }
#endif
}
