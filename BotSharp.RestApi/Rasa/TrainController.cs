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
using System.Threading.Tasks;

namespace BotSharp.RestApi.Rasa
{
#if RASA_UI
    /// <summary>
    /// You can post your training data to this endpoint to train a new model for a project.
    /// This request will wait for the server answer: either the model was trained successfully or the training exited with an error. 
    /// </summary>
    [Route("[controller]")]
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

        /// <summary>
        /// Using the HTTP server, you must specify the project you want to train a new model for to be able to use it during parse requests later on : /train?project=my_project.
        /// </summary>
        /// <param name="agent">Model name</param>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<String>> Train([FromQuery] string agent, [FromQuery] string project)
        {
            string body = "";
            using (var reader = new StreamReader(Request.Body))
            {
                body = reader.ReadToEnd();
            }

            var rasa_nlu_data = JsonConvert.DeserializeObject<RasaTrainRequestModel>(body);
            rasa_nlu_data.Model = agent;
            var trainResult = await Train(rasa_nlu_data, project);

            return trainResult;
        }

        private async Task<ActionResult<String>> Train([FromBody] RasaTrainRequestModel request, [FromQuery] string project)
        {
            var trainer = new BotTrainer();
            if (String.IsNullOrEmpty(request.Project))
            {
                request.Project = project;
            }

            // save corpus to agent dir
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", project);
            var modelPath = Path.Combine(projectPath, request.Model);

            if (!Directory.Exists(modelPath))
            {
                Directory.CreateDirectory(modelPath);
            }

            // Save raw data to file, then parse it to Agent instance.
            // in order to unify the process.
            var fileName = Path.Combine(modelPath, "corpus.json");

            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(request.Corpus, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            var agent = _platform.LoadAgentFromFile<AgentImporterInRasa>(modelPath,
                new AgentImportHeader
                {
                    Id = request.Project,
                    Name = project
                });

            var info = await trainer.Train(agent, new BotTrainOptions { Model = request.Model });

            return Ok(new { info = info.Model });
        }
    }
#endif
}
