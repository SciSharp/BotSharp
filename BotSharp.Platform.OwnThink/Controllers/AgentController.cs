using BotSharp.Platform.Models.Agents;
using BotSharp.Platform.OwnThink.Models;
using BotSharp.Platform.OwnThink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.OwnThink.Controllers
{
    [Route("v1/[controller]")]
    public class AgentController : ControllerBase
    {
        private OwnThinkAi<AgentModel> builder;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(OwnThinkAi<AgentModel> platform)
        {
            builder = platform;
        }

        /// <summary>
        /// Create agent
        /// </summary>
        /// <param name="requestViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(AgentCreationResponseViewModel), 200)]
        public async Task<IActionResult> Create(AgentCreationRequestViewModel requestViewModel)
        {
            var agent = new AgentModel()
            {
                Id = Guid.NewGuid().ToString(),
                Name = requestViewModel.Name,
                Description = requestViewModel.Name,
                AppId = requestViewModel.AppId,
                ClientAccessToken = Guid.NewGuid().ToString("N"),
                DeveloperAccessToken = Guid.NewGuid().ToString("N")
            };

            await builder.SaveAgent(agent);

            return Ok(new AgentCreationResponseViewModel
            {
                AgentId = agent.Id,
                AppId = agent.AppId,
                Name = agent.Name,
                ClientAccessToken = agent.ClientAccessToken
            });
        }
    }
}
