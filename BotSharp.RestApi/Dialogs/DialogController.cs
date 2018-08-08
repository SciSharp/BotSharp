using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi.Dialogs
{
    /// <summary>
    /// Conversation controller
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class DialogController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public DialogController(IBotPlatform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// The query endpoint is used to process natural language in the form of text. 
        /// The query requests return structured data in JSON format with an action and parameters for that action.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/v1/query")]
        public ActionResult<AIResponse> Query([FromBody] QueryModel request)
        {
            String clientAccessToken = Request.Headers["ClientAccessToken"];
            var config = new AIConfiguration(clientAccessToken, SupportedLanguage.English);
            config.SessionId = request.SessionId;

            _platform.LoadAgent(clientAccessToken);

            var aIResponse = _platform.TextRequest(new AIRequest
            {
                Timezone = request.Timezone,
                Contexts = request.Contexts.Select(x => new AIContext { Name = x }).ToList(),
                Language = request.Lang,
                Query = new String[] { request.Query }
            });
            
            return aIResponse;
        }
    }
}
