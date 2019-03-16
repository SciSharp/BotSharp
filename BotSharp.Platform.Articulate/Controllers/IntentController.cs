using BotSharp.Core;
using BotSharp.Platform.Articulate.Models;
using BotSharp.Platform.Articulate.ViewModels;
using BotSharp.Platform.Models;
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
    public class IntentController : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        public IntentController(ArticulateAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpGet("{intentId}")]
        public async Task<IntentViewModel> GetIntent([FromRoute] string intentId)
        {
            var agent = await builder.GetAgentByIntentId(intentId);

            return agent.Item3.ToObject<IntentViewModel>();
        }

        [HttpPost]
        public async Task<IntentViewModel> PostIntent()
        {
            IntentViewModel intent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                intent = JsonConvert.DeserializeObject<IntentViewModel>(body);
            }

            var agent = await builder.GetAgentByName(intent.Agent);
            intent.Id = Guid.NewGuid().ToString();
            var domain = agent.Domains.First(x => x.DomainName == intent.Domain);
            domain.Intents.Add(intent.ToObject<IntentModel>());
            await builder.SaveAgent(agent);

            return intent;
        }

        [HttpPut("{intentId}")]
        public async Task<IntentViewModel> PutIntent([FromRoute] string intentId)
        {
            IntentViewModel intent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                intent = JsonConvert.DeserializeObject<IntentViewModel>(body);
            }

            var agent = await builder.GetAgentByIntentId(intentId);

            var updateAgent = agent.Item1;
            var updateIntents = updateAgent.Domains.First(x => x.Id == agent.Item2.Id).Intents;
            var updateIntent = updateIntents.First(x => x.Id == agent.Item3.Id);

            updateIntent.IntentName = intent.IntentName;
            updateIntent.Examples = intent.Examples;

            await builder.SaveAgent(updateAgent);

            return intent;
        }

        [HttpGet("{intentId}/webhook")]
        public async Task GetIntentWebhook([FromRoute] string intentId)
        {
            
        }

        [HttpGet("{intentId}/postFormat")]
        public async Task GetIntentPostFormat([FromRoute] string intentId)
        {
            
        }

        [HttpGet("/agent/{agentId}/intent")]
        public async Task<IntentPageViewModel> GetAgentIntents([FromRoute] string agentId, [FromQuery] int start, [FromQuery] int limit)
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
        public async Task<List<IntentViewModel>> GetReferencedIntentsByEntity([FromRoute] string entityId, [FromQuery] int start, [FromQuery] int limit)
        {
            var models = await builder.GetReferencedIntentsByEntity(entityId);
            return models.Select(x => x.ToObject<IntentViewModel>()).ToList();
        }

        [HttpGet("/domain/{domainId}/intent")]
        public async Task<IntentPageViewModel> GetReferencedIntentsByDomain([FromRoute] string domainId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = await builder.GetAgentByDomainId(domainId);
            var intents = agent.Item2.Intents.Select(x => x.ToObject<IntentViewModel>()).ToList();

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }
    }
}
