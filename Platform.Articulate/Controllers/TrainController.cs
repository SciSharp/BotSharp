using BotSharp.Core;
using Microsoft.AspNetCore.Mvc;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class TrainController : ControllerBase
    {
        private ArticulateAi<AgentStorageInRedis<AgentModel>, AgentModel> builder;

        public TrainController()
        {
            builder = new ArticulateAi<AgentStorageInRedis<AgentModel>, AgentModel>();
        }

        [HttpGet("/agent/{agentId}/train")]
        public AgentModel TrainAgent([FromRoute] string agentId)
        {
            var agent = builder.GetAgentById(agentId);

            agent.Status = "ready";

            return agent;
        }
    }
#endif
}
