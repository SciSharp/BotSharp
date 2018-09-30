using BotSharp.Core;
using BotSharp.Platform.Models;
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
        private ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel> builder;

        public IntentController()
        {
            builder = new ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel>();
        }

        [HttpGet("{intentId}")]
        public IntentModel GetIntent([FromRoute] string intentId)
        {
            var agent = builder.GetAgentByIntentId(intentId);

            return agent.Item3;
        }

        [HttpPost]
        public IntentModel PostIntent()
        {
            IntentModel intent = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                intent = JsonConvert.DeserializeObject<IntentModel>(body);
            }

            var agent = builder.GetAgentByName(intent.Agent);
            intent.Id = Guid.NewGuid().ToString();
            agent.Domains[0].Intents.Add(intent);
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
            var intents = new List<IntentModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agentId}-intent-")).ToList();
            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var intent = JsonConvert.DeserializeObject<IntentModel>(json);

                intents.Add(intent);
            }

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }

        [HttpGet("/entity/{entityId}/intent")]
        public List<IntentModel> GetReferencedIntentsByEntity([FromRoute] string entityId, [FromQuery] int start, [FromQuery] int limit)
        {
            var intents = new List<IntentModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string entityPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.Contains($"-entity-{entityId}.json"));
            var entity = JsonConvert.DeserializeObject<EntityModel>(System.IO.File.ReadAllText(entityPath));

            string agentId = entityPath.Split(Path.DirectorySeparatorChar).Last().Split('-')[1];

            string agentPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.Contains($"agent-{agentId}.json"));
            var agent = JsonConvert.DeserializeObject<AgentModel>(System.IO.File.ReadAllText(agentPath));

            var intentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agent.Id}-intent-")).ToList();

            for (int i = 0; i < intentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(intentPaths[i]);

                var intent = JsonConvert.DeserializeObject<IntentModel>(json);

                if (intent.Examples.Exists(x => x.Entities.Exists(e => e.EntityId == entityId)))
                {
                    intents.Add(intent);
                }
            }

            return intents;
        }

        [HttpGet("/domain/{domainId}/intent")]
        public IntentPageViewModel GetReferencedIntentsByDomain([FromRoute] string domainId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = builder.GetAgentByDomainId(domainId);
            var intents = agent.Item2.Intents.Select(x => x as IntentModel).ToList();

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }
    }
#endif
}
