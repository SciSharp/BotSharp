using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using BotSharp.Platform.Dialogflow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Platform.Models.MachineLearning;
using Ding.Log;
using Ding.Serialization;

namespace BotSharp.Platform.Dialogflow.Controllers
{
    /// <summary>
    /// You can post your training data to this endpoint to train a new model for a project.
    /// This request will wait for the server answer: either the model was trained successfully or the training exited with an error. 
    /// </summary>
    [Route("v1/[controller]")]
    public class TrainController : ControllerBase
    {
        private DialogflowAi<AgentModel> builder;

        public TrainController(DialogflowAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpPost]
        public async Task<ActionResult<ModelMetaData>> Train([FromQuery] string agentId)
        {
            var agent = await builder.GetAgentById(agentId);

            if(agent == null)
            {
                agent = await builder.GetAgentByName(agentId);
            }

            var corpus = await builder.ExtractorCorpus(agent);

            var meta = await builder.Train(agent, corpus, new BotTrainOptions { });

            return meta;
        }
    }
}
