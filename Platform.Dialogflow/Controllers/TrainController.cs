using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Platform.Dialogflow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Platform.Dialogflow.Controllers
{
#if DIALOGFLOW
    /// <summary>
    /// You can post your training data to this endpoint to train a new model for a project.
    /// This request will wait for the server answer: either the model was trained successfully or the training exited with an error. 
    /// </summary>
    [Route("v1/[controller]")]
    public class TrainController : ControllerBase
    {
        private DialogflowAi<AgentModel> builder;

        public TrainController(IConfiguration configuration)
        {
            builder = new DialogflowAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("DialogflowAi");
        }

        [HttpPost]
        public async Task<ActionResult<AgentModel>> Train([FromQuery] string agentId)
        {
            var agent = builder.GetAgentById(agentId);

            if(agent == null)
            {
                agent = builder.GetAgentByName(agentId);
            }

            var corpus = builder.ExtractorCorpus(agent);

            await builder.Train(agent, corpus);

            return agent;
        }
    }
#endif
}
