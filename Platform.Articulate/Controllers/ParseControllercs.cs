using BotSharp.NLP;
using BotSharp.Platform.Models.AiRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Platform.Articulate.Models;
using Platform.Articulate.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Console = Colorful.Console;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class ParseControllercs : ControllerBase
    {
        private readonly IConfiguration configuration;
        private ArticulateAi<AgentModel> builder;

        public ParseControllercs(IConfiguration configuration)
        {
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpGet("/agent/{agentId}/converse")]
        public ActionResult ParseText([FromRoute] string agentId, [FromQuery] string text, [FromQuery] string sessionId)
        {
            var response = new ResponseViewModel();

            Console.WriteLine($"Got message from {Request.Host}: {text}", Color.Green);

            var aiResponse = builder.TextRequest(new AiRequest
            {
                AgentId = agentId,
                Text = text
            });

            response.TextResponse = aiResponse.Speech;

            return Ok(response);
        }
    }
#endif
}
