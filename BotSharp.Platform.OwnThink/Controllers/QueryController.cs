using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.OwnThink.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.OwnThink.Controllers
{
    [Route("v1/[controller]")]
    public class QueryController : ControllerBase
    {
        private OwnThinkAi<AgentModel> builder;

        public QueryController(OwnThinkAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpGet, HttpPost]
        public async Task<ActionResult<OwnThinkAiResponse>> Query(OwnThinkAiRequest request)
        {
            String clientAccessToken = (User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

            // find a model according to clientAccessToken

            var agents = await builder.GetAllAgents();
            var agent = agents.FirstOrDefault(x => x.ClientAccessToken == clientAccessToken);

            if (agent == null)
            {
                return BadRequest("The agent not found.");
            }

            var aIResponse = await builder.TextRequest<AIResponseResult>(new AiRequest
            {
                Text = request.Spoken,
                AgentId = agent.Id,
                SessionId = request.AppId
            });

            return new OwnThinkAiResponse
            {

            };
        }
    }
}
