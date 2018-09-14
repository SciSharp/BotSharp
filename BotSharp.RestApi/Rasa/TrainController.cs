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
#if RASA
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
        /// <param name="model">Model name</param>
        /// <param name="project">Agent name or agent id</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<String>> Train([FromQuery] string project, [FromQuery] string model)
        {
            string body = "";
            using (var reader = new StreamReader(Request.Body))
            {
                body = reader.ReadToEnd();
            }

            string lang = Regex.Match(body, @"language:.+")?.Value;
            if (!String.IsNullOrEmpty(lang))
            {
                lang = lang.Substring(11, 2);
            }
            string data = Regex.Match(body, @"data:([\s\S]*)")?.Value;
            if (String.IsNullOrEmpty(data))
            {
                data = body;
            }
            else
            {
                data = data.Substring(6);
            }

            var rasa_nlu_data = JsonConvert.DeserializeObject<RasaTrainRequestModel>(data);
            rasa_nlu_data.Model = model;
            rasa_nlu_data.Project = project;
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
            var metaFileName = Path.Combine(modelPath, "meta.json");
            System.IO.File.WriteAllText(metaFileName, JsonConvert.SerializeObject(new AgentImportHeader
            {
                Name = project,
                Platform = PlatformType.Rasa
            }));
            // in order to unify the process.
            var fileName = Path.Combine(modelPath, "corpus.json");

            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(request.Corpus, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            var agent = _platform.LoadAgentFromFile(modelPath);

            var info = await trainer.Train(agent, new BotTrainOptions
            {
                AgentDir = projectPath,
                Model = request.Model
            });

            return Ok(new { info = info.Model });
        }
    }
#endif
}
