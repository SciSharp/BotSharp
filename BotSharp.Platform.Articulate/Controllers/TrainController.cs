using BotSharp.Platform.Articulate.Models;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class TrainController : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        public TrainController(ArticulateAi<AgentModel> articulateAi)
        {
            builder = articulateAi;
        }

        [HttpGet("/agent/{agentId}/train")]
        public async Task<AgentModel> TrainAgent([FromRoute] string agentId)
        {
            var agent = await builder.GetAgentById(agentId);

            var corpus = await builder.ExtractorCorpus(agent);

            await builder.Train(agent, corpus, new BotTrainOptions { });

            agent.Status = "Ready";

           await  builder.SaveAgent(agent);

            return agent;
        }
    }
}
