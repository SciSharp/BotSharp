using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.Rasa;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.RestApi.Rasa
{
#if MODE_RASA
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

        [HttpPost]
        public async Task<ActionResult<String>> Train([FromBody] RasaTrainRequestModel request, [FromQuery] string project)
        {
            var trainer = new BotTrainer();
            if (String.IsNullOrEmpty(request.Project))
            {
                request.Project = project;
            }

            // save corpus to agent dir
            var projectPath = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");
            var dataPath = Path.Join(projectPath, project);
            var agentPath = Path.Join(dataPath, "Temp");

            if (!Directory.Exists(agentPath))
            {
                Directory.CreateDirectory(agentPath);
            }

            var fileName = Path.Join(agentPath, "corpus.json");

            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(request.Corpus, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            var bot = new RasaAi();
            var agent = bot.LoadAgentFromFile<AgentImporterInRasa>(agentPath,
                new AgentImportHeader
                {
                    Id = request.Project,
                    Name = project
                });

            // convert agent to training corpus
            agent.Corpus = new TrainingCorpus
            {
                Entities = new List<TrainingEntity>(),
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>()
            };

            agent.Intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(say => {
                    agent.Corpus.UserSays.Add(new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", say.Data.Select(x => x.Text)),
                        Entities = say.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                        .Select(x => new TrainingIntentExpressionPart {
                            Value = x.Text,
                            Entity = x.Meta
                        })
                        .ToList()
                    });
                });
            });

            string info = await trainer.Train(agent);

            return Ok(new { info });
        }
    }
#endif
}
