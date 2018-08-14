using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi.Dialogflow
{
#if MODE_DIALOGFLOW
    /// <summary>
    /// Dialogflow mode query controller
    /// </summary>
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
            String clientAccessToken = Request.Headers["ClientAccessToken"];
            var config = new AIConfiguration(clientAccessToken, SupportedLanguage.English);
            config.SessionId = request.SessionId;

            _platform.LoadAgent(clientAccessToken);

            var aIResponse = _platform.TextRequest(new AIRequest
            {
                Timezone = request.Timezone,
                Contexts = request?.Contexts?.Select(x => new AIContext { Name = x })?.ToList(),
                Language = request.Lang,
                Query = new String[] { request.Query }
            });

            return aIResponse;
        }
    }
#endif
}
