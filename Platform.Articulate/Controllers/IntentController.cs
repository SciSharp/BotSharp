using BotSharp.Core;
using BotSharp.Platform.Models;
using DotNetToolkit;
using Microsoft.AspNetCore.Mvc;
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
#if ARTICULATE
    [Route("[controller]")]
    public class IntentController : ControllerBase
    {
        private ArticulateAi<AgentStorageInRedis<AgentModel>, AgentModel> builder;

        public IntentController()
        {
            builder = new ArticulateAi<AgentStorageInRedis<AgentModel>, AgentModel>();
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
#endif
}
