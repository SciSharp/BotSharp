using BotSharp.Core;
using DotNetToolkit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class ScenarioController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private ArticulateAi<AgentModel> builder;

        public ScenarioController(IConfiguration configuration)
        {
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpGet("/intent/{intentId}/scenario")]
        public IntentScenarioViewModel GetIntentScenario([FromRoute] string intentId)
        {
            var agent = builder.GetAgentByIntentId(intentId);

            var view = agent.Item3.Scenario.ToObject<IntentScenarioViewModel>();

            view.Agent = agent.Item1.AgentName;
            view.Domain = agent.Item2.DomainName;
            view.Intent = agent.Item3.IntentName;

            return view;
        }

        [HttpPost("/intent/{intentId}/scenario")]
        public IntentScenarioViewModel PostIntentScenario()
        {
            IntentScenarioViewModel scenario = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                scenario = JsonConvert.DeserializeObject<IntentScenarioViewModel>(body);
            }

            scenario.Id = Guid.NewGuid().ToString();

            var agent = builder.GetAgentByName(scenario.Agent);

            var domain = agent.Domains.FirstOrDefault(x => x.DomainName == scenario.Domain);
            var intent = domain.Intents.FirstOrDefault(x => x.IntentName == scenario.Intent);
            intent.Scenario = scenario.ToObject<ScenarioModel>();

            builder.SaveAgent(agent);

            return scenario;
        }
    }
#endif
}
