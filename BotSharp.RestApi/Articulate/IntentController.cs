using BotSharp.Core.Engines.Articulate;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi.Articulate
{
#if ARTICULATE
    [Route("[controller]")]
    public class IntentController : ControllerBase
    {
        [HttpGet("{intentId}")]
        public IntentModel GetIntent([FromRoute] int intentId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.EndsWith($"-intent-{intentId}.json"));

            string json = System.IO.File.ReadAllText(dataPath);

            var intent = JsonConvert.DeserializeObject<IntentModel>(json);

            return intent;
        }

        [HttpGet("{intentId}/scenario")]
        public IntentScenarioModel GetIntentScenario([FromRoute] int intentId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.Contains($"-intent-{intentId}-scenario-"));

            string json = System.IO.File.ReadAllText(dataPath);

            var scenario = JsonConvert.DeserializeObject<IntentScenarioModel>(json);

            return scenario;
        }

        [HttpGet("{intentId}/webhook")]
        public void GetIntentWebhook([FromRoute] int intentId)
        {
            
        }

        [HttpGet("{intentId}/postFormat")]
        public void GetIntentPostFormat([FromRoute] int intentId)
        {
            
        }

        [HttpGet("/agent/{agentId}/intent")]
        public IntentPageViewModel GetAgentIntents([FromRoute] int agentId, [FromQuery] int start, [FromQuery] int limit)
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
        public List<IntentModel> GetReferencedIntentsByEntity([FromRoute] int entityId, [FromQuery] int start, [FromQuery] int limit)
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
        public IntentPageViewModel GetReferencedIntentsByDomain([FromRoute] int domainId, [FromQuery] int start, [FromQuery] int limit)
        {
            var intents = new List<IntentModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string domainPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.Contains($"-domain-{domainId}.json"));
            var domain = JsonConvert.DeserializeObject<DomainModel>(System.IO.File.ReadAllText(domainPath));

            string agentId = domainPath.Split(Path.DirectorySeparatorChar).Last().Split('-')[1];

            string agentPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.Contains($"agent-{agentId}.json"));
            var agent = JsonConvert.DeserializeObject<AgentModel>(System.IO.File.ReadAllText(agentPath));

            var intentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agentId}-intent-")).ToList();

            for (int i = 0; i < intentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(intentPaths[i]);

                var intent = JsonConvert.DeserializeObject<IntentModel>(json);

                if (intent.Domain == domain.DomainName)
                {
                    intents.Add(intent);
                }
            }

            return new IntentPageViewModel { Intents = intents, Total = intents.Count };
        }
    }
#endif
}
