using BotSharp.Platform.Rasa.Models;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Platform.Rasa.Controllers
{
    /// <summary>
    /// send a text request
    /// </summary>
    [Route("[controller]")]
    public class ParseController : ControllerBase
    {
        private readonly RasaAi<AgentModel> builder;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public ParseController(RasaAi<AgentModel> configuration)
        {
            builder = configuration;
        }

        /// <summary>
        /// parse request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, HttpGet]
        public ActionResult<RasaResponse> Parse(RasaRequestModel request)
        {
            /*var config = new AIConfiguration("", SupportedLanguage.English);
            config.SessionId = "rasa nlu";

            string body = "";
            using (var reader = new StreamReader(Request.Body))
            {
                body = reader.ReadToEnd();
            }

            Console.WriteLine($"Got message from {Request.Host}: {body}", Color.Green);
            if(request.Project ==null && !String.IsNullOrEmpty(body))
            {
                request = JsonConvert.DeserializeObject<RasaRequestModel>(body);
            }

            // Load agent
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", request.Project);

            if (String.IsNullOrEmpty(request.Model))
            {
                request.Model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            }

            var modelPath = Path.Combine(projectPath, request.Model);

            var agent = _platform.LoadAgentFromFile(modelPath);

            var aIResponse = _platform.TextRequest(new AIRequest
            {
                AgentDir = projectPath,
                Model = request.Model,
                Query = new String[] { request.Text }
            });

            var rasaResponse = new RasaResponse
            {
                Intent = new RasaResponseIntent
                {
                    Name = aIResponse.Result.Metadata.IntentName,
                    Confidence = aIResponse.Result.Score
                },
                Entities = aIResponse.Result.Entities.Select(x => new RasaResponseEntity
                {
                    Extractor = x.Extrator,
                    Start = x.Start,
                    Entity = x.Entity,
                    Value = x.Value
                }).ToList(),
                Text = request.Text,
                Model = request.Model,
                Project = agent.Name,
                IntentRanking = new List<RasaResponseIntent>
                {
                    new RasaResponseIntent
                    {
                        Name = aIResponse.Result.Metadata.IntentName,
                        Confidence = aIResponse.Result.Score
                    }
                },
                Fullfillment = aIResponse.Result.Fulfillment
            };

            return rasaResponse;*/

            return null;
        }
    }
}
