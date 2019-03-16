using BotSharp.Platform.Articulate.Models;
using BotSharp.Platform.Articulate.ViewModels;
using BotSharp.Platform.Models.AiRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace BotSharp.Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class ParseControllercs : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        public ParseControllercs(ArticulateAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpGet("/agent/{agentId}/converse")]
        public async Task<ActionResult> ParseText([FromRoute] string agentId, [FromQuery] string text, [FromQuery] string sessionId)
        {
            var response = new ResponseViewModel();

            Console.WriteLine($"Got message from {Request.Host}: {text}", Color.Green);

            var aiResponse = await builder.TextRequest(new AiRequest
            {
                AgentId = agentId,
                Text = text
            });

            response.TextResponse = aiResponse.Text;

            return Ok(response);
        }
    }
}
