using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.Rasa;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotSharp.RestApi.Rasa
{
#if DIALOGFLOW
    /// <summary>
    /// You can post your training data to this endpoint to train a new model for a project.
    /// This request will wait for the server answer: either the model was trained successfully or the training exited with an error. 
    /// </summary>
    [Route("v1/[controller]")]
    public class TrainController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public TrainController(IBotPlatform platform)
        {
            _platform = platform;
        }

        [HttpPost]
        public async Task<ActionResult<String>> Train([FromQuery] string agentId)
        {
            var trainer = new BotTrainer();

            // save corpus to agent dir
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agentId);
            var model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            string dataDir = Path.Combine(projectPath, model);
            
            var agent = _platform.LoadAgentFromFile(dataDir);

            var info = await trainer.Train(agent, new BotTrainOptions
            {
                AgentDir = projectPath,
                Model = model
            });

            return Ok(new { info = info });
        }
    }
#endif
}
