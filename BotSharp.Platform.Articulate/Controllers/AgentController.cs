using BotSharp.Platform.Articulate.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotSharp.Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class AgentController : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        /// <summary>
        /// Initialize agent controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(ArticulateAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpPost]
        public async Task<AgentModel> PostAgent()
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
            await builder.SaveAgent(agent);

            return agent;
        }

        [HttpGet]
        public async Task<List<AgentModel>> GetAgent()
        {
            var results = await builder.GetAllAgents();
            var agents = results.ToList();

            return agents;
        }

        [HttpGet("{agentId}")]
        public async Task<AgentModel> GetAgentById([FromRoute] string agentId)
        {
            var agent = await builder.GetAgentById(agentId);
            
            return agent;
        }

        [HttpGet("name/{agentName}")]
        public async Task<AgentModel> GetAgentByName([FromRoute] string agentName)
        {
            var agent = await builder.GetAgentByName(agentName);
            
            return agent;
        }
    }
}
