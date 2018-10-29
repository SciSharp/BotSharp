using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Dialogflow.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using BotSharp.Platform.Models.AiRequest;
using System.Threading.Tasks;

namespace BotSharp.Platform.Dialogflow.Controllers
{
    /// <summary>
    /// Dialogflow mode query controller
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class QueryController : ControllerBase
    {
        private DialogflowAi<AgentModel> builder;

        public QueryController(DialogflowAi<AgentModel> platform)
        {
            builder = platform;
        }

        /// <summary>
        /// The query endpoint is used to process natural language in the form of text. 
        /// The query requests return structured data in JSON format with an action and parameters for that action.
        /// Both GET and POST methods return the same JSON response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public async Task<ActionResult<AIResponse>> Query(QueryModel request)
        {
            String clientAccessToken = (User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

            // find a model according to clientAccessToken

            var agents = await builder.GetAllAgents();
            var agent = agents.FirstOrDefault(x => x.ClientAccessToken == clientAccessToken);

            if(agent == null)
            {
                return BadRequest("The agent not found.");
            }

            var aIResponse = await builder.TextRequest<AIResponseResult>(new AiRequest
            {
                Text = request.Query,
                AgentId = agent.Id,
                SessionId = request.SessionId
            });

            return new AIResponse
            {
                Result = aIResponse,
                Id = Guid.NewGuid().ToString(),
                Lang = request.Lang,
                SessionId = request.SessionId,
                Status = new AIResponseStatus(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
