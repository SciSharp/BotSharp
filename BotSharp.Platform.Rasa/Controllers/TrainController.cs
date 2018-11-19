using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.MachineLearning;
using BotSharp.Platform.Rasa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

namespace BotSharp.Platform.Rasa.Controllers
{
    /// <summary>
    /// You can post your training data to this endpoint to train a new model for a project.
    /// This request will wait for the server answer: either the model was trained successfully or the training exited with an error. 
    /// </summary>
    [Route("[controller]")]
    public class TrainController : ControllerBase
    {
        private RasaAi<AgentModel> builder;
        private readonly IPlatformSettings settings;

        public TrainController(RasaAi<AgentModel> configuration, IPlatformSettings settings)
        {
            builder = configuration;
            this.settings = settings;
        }

        /// <summary>
        /// Using the HTTP server, you must specify the project you want to train a new model for to be able to use it during parse requests later on : /train?project=my_project.
        /// </summary>
        /// <param name="model">Model name</param>
        /// <param name="project">Agent name or agent id</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ModelMetaData>> Train([FromQuery] string project, [FromQuery] string model)
        {
            string agentDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", project);
            if (!Directory.Exists(agentDir))
            {
                Directory.CreateDirectory(agentDir);
            }

            string body = "";
            using (var reader = new StreamReader(Request.Body))
            {
                body = reader.ReadToEnd();
            }

            var agent = await ImportAgent(project, body);

            var corpus = await builder.ExtractorCorpus(agent);

            var meta = await builder.Train(agent, corpus, new BotTrainOptions { Model = model });

            return meta;

        }

        private async Task<AgentModel> ImportAgent(string project, string body)
        {
            Console.WriteLine($"Update agent from http post, data length: {body.Length}");

            // save to file
            // save corpus to agent dir
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", project);
            var rawPath = Path.Combine(projectPath, "tmp");

            // clear tmp dir
            if (Directory.Exists(rawPath))
            {
                Directory.Delete(rawPath, true);
            }

            Directory.CreateDirectory(rawPath);

            // Save raw data to file, then parse it to Agent instance.
            var metaFileName = Path.Combine(rawPath, "meta.json");
            System.IO.File.WriteAllText(metaFileName, JsonConvert.SerializeObject(new AgentImportHeader
            {
                Name = project,
                Platform = PlatformType.Rasa,
                Id = Guid.NewGuid().ToString()
            }, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            // in order to unify the process.
            var fileName = Path.Combine(rawPath, "corpus.json");

            System.IO.File.WriteAllText(fileName, body);

            /*string lang = Regex.Match(body, @"language:.+")?.Value;
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
            }*/

            /*var agent = builder.GetAgentById(project);

            if (agent == null)
            {
                agent = builder.GetAgentByName(project);
            }

            var corpus = builder.ExtractorCorpus(agent);

            var meta = await builder.Train(agent, corpus);*/

            // var rasa_nlu_data = JsonConvert.DeserializeObject<RasaTrainRequestModel>(data);
            //rasa_nlu_data.Model = model;
            //rasa_nlu_data.Project = project;

            var agent = await builder.LoadAgentFromFile<AgentImporterInRasa<AgentModel>>(rawPath);
            await builder.SaveAgent(agent);

            return agent;
        }

        private async Task<ActionResult<String>> Train([FromBody] RasaTrainRequestViewModel request, [FromQuery] string project)
        {
            var trainer = new BotTrainer(settings);
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
                Name = project
            }));
            // in order to unify the process.
            var fileName = Path.Combine(modelPath, "corpus.json");

            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            var agent = await builder.GetAgentByName(project);

            var info = await trainer.Train(agent, new BotTrainOptions
            {
                AgentDir = projectPath,
                Model = request.Model
            });

            return Ok(new { info = info.Model });
        }
    }
}
