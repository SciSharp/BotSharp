using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using BotSharp.NLP;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
#if MODE_RASA
    [Route("[controller]")]
    public class ParseController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public ParseController(IBotPlatform platform)
        {
            _platform = platform;
        }

        [HttpPost]
        public ActionResult<RasaResponse> Parse(RasaRequestModel request)
        {
            String clientAccessToken = Request.Headers["ClientAccessToken"];
            var config = new AIConfiguration(clientAccessToken, SupportedLanguage.English);
            config.SessionId = "rasa nlu";

            _platform.LoadAgent(clientAccessToken);

            var aIResponse = _platform.TextRequest(new AIRequest
            {
                Query = new String[] { request.Text }
            });

            return new RasaResponse
            {
                Intent = new RasaResponseIntent
                {
                    Name = aIResponse.Result.Metadata.IntentName,
                    Confidence = aIResponse.Result.Score
                },
                Entities = new List<RasaResponseEntity>
                {

                },
                Text = request.Text
            };
        }
    }
#endif
}
