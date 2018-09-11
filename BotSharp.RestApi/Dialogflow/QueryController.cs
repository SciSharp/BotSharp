using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using BotSharp.NLP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace BotSharp.RestApi.Dialogflow
{
#if DIALOGFLOW
    /// <summary>
    /// Dialogflow mode query controller
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public QueryController(IBotPlatform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// The query endpoint is used to process natural language in the form of text. 
        /// The query requests return structured data in JSON format with an action and parameters for that action.
        /// Both GET and POST methods return the same JSON response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public ActionResult<AIResponse> Query(QueryModel request)
        {
            String clientAccessToken = (User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
            _platform.AiConfig = new AIConfiguration(clientAccessToken, SupportedLanguage.English);
            _platform.AiConfig.SessionId = request.SessionId;

            // find a model according to clientAccessToken
            string projectPath = String.Empty;
            string projectsPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");

            string[] d1 = Directory.GetDirectories(projectsPath);
            for (int i = 0; i < d1.Length; i++)
            {
                string[] d2 = Directory.GetDirectories(d1[i]);
                for (int j = 0; j < d2.Length; j++)
                {
                    string metaJson = System.IO.File.ReadAllText(Path.Combine(d2[j], "meta.json"));

                    var meta = JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);

                    if (meta.ClientAccessToken == clientAccessToken)
                    {
                        projectPath = d1[i];
                        break;
                    }
                };

                if (!String.IsNullOrEmpty(projectPath))
                {
                    break;
                }
            };

            // Load agent
            string model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            string dataDir = Path.Combine(projectPath, model);

            _platform.LoadAgentFromFile(dataDir);

            var aIResponse = _platform.TextRequest(new AIRequest
            {
                Timezone = request.Timezone,
                Contexts = request?.Contexts?.Select(x => new AIContext { Name = x })?.ToList(),
                Language = request.Lang,
                Query = new String[] { request.Query },
                AgentDir = projectPath,
                Model = model
            });

            return aIResponse;
        }
    }
#endif
}
