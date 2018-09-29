using BotSharp.Core;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Platform.Articulate.Models;
using Platform.Articulate.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class DomainController : ControllerBase
    {
        private ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel> builder;

        public DomainController()
        {
            builder = new ArticulateAi<AgentStorageInMemory<AgentModel>, AgentModel>();
        }

        [HttpGet("{domainId}")]
        public DomainModel GetDomain([FromRoute] int domainId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => Regex.IsMatch(x, $"-domain-{domainId}.json"));
            string json = System.IO.File.ReadAllText(dataPath);

            var domain = JsonConvert.DeserializeObject<DomainModel>(json);

            return domain;
        }

        [HttpPost]
        public DomainModel PostDomain()
        {
            DomainModel domain = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                domain = JsonConvert.DeserializeObject<DomainModel>(body);
            }

            var agent = builder.GetAgentByName(domain.Agent);
            domain.Id = Guid.NewGuid().ToString();
            (agent as AgentModel).Domains.Add(domain);
            builder.SaveAgent(agent);

            return domain;
        }

        [HttpGet("/agent/{agentId}/domain")]
        public DomainPageViewModel GetAgentDomains([FromRoute] string agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = builder.GetAgentById(agentId);
            

            return new DomainPageViewModel { /*Domains = agent.d, Total = domains.Count*/ };
        }
    }
#endif
}
