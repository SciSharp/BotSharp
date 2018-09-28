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
    public class EntityController : ControllerBase
    {
        [HttpGet("{entityId}")]
        public EntityModel GetEntity([FromRoute] int entityId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.EndsWith($"-entity-{entityId}.json"));

            string json = System.IO.File.ReadAllText(dataPath);

            var entity = JsonConvert.DeserializeObject<EntityModel>(json);

            return entity;
        }

        [HttpGet("/agent/{agentId}/entity")]
        public EntityPageViewModel GetAgentEntities([FromRoute] int agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var entities = new List<EntityModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agentId}-entity-")).ToList();
            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var entity = JsonConvert.DeserializeObject<EntityModel>(json);

                entities.Add(entity);
            }

            return new EntityPageViewModel { Entities = entities, Total = entities.Count };
        }
    }
#endif
}
