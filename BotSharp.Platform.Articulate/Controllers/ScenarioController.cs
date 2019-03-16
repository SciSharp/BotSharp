using BotSharp.Core;
using BotSharp.Platform.Articulate.Models;
using DotNetToolkit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class ScenarioController : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        public ScenarioController(ArticulateAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpGet("/intent/{intentId}/scenario")]
        public async Task<IntentScenarioViewModel> GetIntentScenario([FromRoute] string intentId)
        {
            var agent = await builder.GetAgentByIntentId(intentId);

            var view = agent.Item3.Scenario.ToObject<IntentScenarioViewModel>();

            view.Agent = agent.Item1.AgentName;
            view.Domain = agent.Item2.DomainName;
            view.Intent = agent.Item3.IntentName;

            return view;
        }

        [HttpPost("/intent/{intentId}/scenario")]
        public async Task<IntentScenarioViewModel> PostIntentScenario()
        {
            IntentScenarioViewModel scenario = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                scenario = JsonConvert.DeserializeObject<IntentScenarioViewModel>(body);
            }

            scenario.Id = Guid.NewGuid().ToString();

            var agent = await builder.GetAgentByName(scenario.Agent);

            var domain = agent.Domains.FirstOrDefault(x => x.DomainName == scenario.Domain);
            var intent = domain.Intents.FirstOrDefault(x => x.IntentName == scenario.Intent);
            intent.Scenario = scenario.ToObject<ScenarioModel>();

            await builder.SaveAgent(agent);

            return scenario;
        }
    }
}
