using BotSharp.Core;
using BotSharp.Platform.Models;
using DotNetToolkit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.Articulate.Models;
using Platform.Articulate.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class IntentController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private ArticulateAi<AgentModel> builder;

        public IntentController(IConfiguration configuration)
        {
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpGet("{intentId}")]
        public IntentViewModel GetIntent([FromRoute] string intentId)
        {
            var agent = builder.GetAgentByIntentId(intentId);

            return agent.Item3.ToObject<IntentViewModel>();
        }

        [HttpPost]
        public IntentViewModel PostIntent()
        {
            IntentViewModel intent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                intent = JsonConvert.DeserializeObject<IntentViewModel>(body);
            }

            var agent = builder.GetAgentByName(intent.Agent);
            intent.Id = Guid.NewGuid().ToString();
            var domain = agent.Domains.First(x => x.DomainName == intent.Domain);
            domain.Intents.Add(intent.ToObject<IntentModel>());
            builder.SaveAgent(agent);

            return intent;
        }

        [HttpPut("{intentId}")]
        public IntentViewModel PutIntent([FromRoute] string intentId)
        {
            IntentViewModel intent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                intent = JsonConvert.DeserializeObject<IntentViewModel>(body);
            }

            var agent = builder.GetAgentByIntentId(intentId);

            var updateAgent = agent.Item1;
            var updateIntents = updateAgent.Domains.First(x => x.Id == agent.Item2.Id).Intents;
            var updateIntent = updateIntents.First(x => x.Id == agent.Item3.Id);

            updateIntent.IntentName = intent.IntentName;
            updateIntent.Examples = intent.Examples;

            builder.SaveAgent(updateAgent);

            return intent;
        }

        [HttpGet("{intentId}/webhook")]
        public void GetIntentWebhook([FromRoute] string intentId)
        {
            
        }

        [HttpGet("{intentId}/postFormat")]
        public void GetIntentPostFormat([FromRoute] string intentId)
        {
            
        }

        [HttpGet("/agent/{agentId}/intent")]
        public IntentPageViewModel GetAgentIntents([FromRoute] string agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var intents = new List<IntentViewModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agentId}-intent-")).ToList();
            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var intent = JsonConvert.DeserializeObject<IntentViewModel>(json);

                intents.Add(intent);
            }

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }

        [HttpGet("/entity/{entityId}/intent")]
        public List<IntentViewModel> GetReferencedIntentsByEntity([FromRoute] string entityId, [FromQuery] int start, [FromQuery] int limit)
        {
            return builder.GetReferencedIntentsByEntity(entityId).Select(x => x.ToObject<IntentViewModel>()).ToList();
        }

        [HttpGet("/domain/{domainId}/intent")]
        public IntentPageViewModel GetReferencedIntentsByDomain([FromRoute] string domainId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = builder.GetAgentByDomainId(domainId);
            var intents = agent.Item2.Intents.Select(x => x.ToObject<IntentViewModel>()).ToList();

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }
    }
}
