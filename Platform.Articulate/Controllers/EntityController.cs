using BotSharp.Core;
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

            return new EntityPageViewModel { Entities = entities, Total = entities.Count };
        }

        [HttpPost]
        public EntityModel PostEntity()
        {
            EntityModel entity = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                entity = JsonConvert.DeserializeObject<EntityModel>(body);
            }

            var builder = new ArticulateAi<AgentStorageInMemory<DomainModel, EntityModel>, AgentModel, DomainModel, EntityModel>();
            var agent = builder.GetAgentByName(entity.Agent);

            agent.Entities.Add(entity);

            builder.SaveAgent(agent);

            return entity;
        }
    }
#endif
}
