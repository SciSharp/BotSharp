using BotSharp.Core;
using BotSharp.Platform.Models;
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
#if ARTICULATE
    [Route("[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private ArticulateAi<AgentModel> builder;

        public EntityController(IConfiguration configuration)
        {
            builder = new ArticulateAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("ArticulateAi");
        }

        [HttpGet("{entityId}")]
        public EntityModel GetEntity([FromRoute] string entityId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            string dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => x.EndsWith($"-entity-{entityId}.json"));

            string json = System.IO.File.ReadAllText(dataPath);

            var entity = JsonConvert.DeserializeObject<EntityModel>(json);

            return entity;
        }

        [HttpGet("/agent/{agentId}/entity")]
        public EntityPageViewModel GetAgentEntities([FromRoute] string agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = builder.GetAgentById(agentId);
            return new EntityPageViewModel { Entities = agent.Entities.Select(x => x as EntityModel).ToList(), Total = agent.Entities.Count };
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

            var agent = builder.GetAgentByName(entity.Agent);
            entity.Id = Guid.NewGuid().ToString();
            agent.Entities.Add(entity);

            builder.SaveAgent(agent);

            return entity;
        }
    }
#endif
}
