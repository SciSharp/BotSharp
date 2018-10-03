using BotSharp.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class TrainController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private ArticulateAi<AgentModel> builder;

        public TrainController(IConfiguration configuration)
        {
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpGet("/agent/{agentId}/train")]
        public async Task<AgentModel> TrainAgent([FromRoute] string agentId)
        {
            var agent = builder.GetAgentById(agentId);

            var corpus = builder.ExtractorCorpus(agent);

            await builder.Train(agent, corpus);

            agent.Status = "Ready";

            builder.SaveAgent(agent);

            return agent;
        }
    }
}
