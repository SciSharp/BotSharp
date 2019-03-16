using BotSharp.Platform.Articulate.Models;
using BotSharp.Platform.Articulate.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotSharp.Platform.Articulate.Controllers
{
    [Route("[controller]")]
    public class DomainController : ControllerBase
    {
        private ArticulateAi<AgentModel> builder;

        public DomainController(ArticulateAi<AgentModel> platform)
        {
            builder = platform;
        }

        [HttpGet("{domainId}")]
        public async Task<DomainModel> GetDomain([FromRoute] int domainId)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var dataPath = Directory.GetFiles(dataDir).FirstOrDefault(x => Regex.IsMatch(x, $"-domain-{domainId}.json"));
            string json = System.IO.File.ReadAllText(dataPath);

            var domain = JsonConvert.DeserializeObject<DomainModel>(json);

            return domain;
        }

        [HttpPost]
        public async Task<DomainModel> PostDomain()
        {
            DomainModel domain = null;

            using (var reader = new StreamReader(Request.Body))
            {
                string body = reader.ReadToEnd();
                domain = JsonConvert.DeserializeObject<DomainModel>(body);
            }

            var agent = await builder.GetAgentByName(domain.Agent);
            domain.Id = Guid.NewGuid().ToString();
            (agent as AgentModel).Domains.Add(domain);
            await builder.SaveAgent(agent);

            return domain;
        }

        [HttpGet("/agent/{agentId}/domain")]
        public async Task<DomainPageViewModel> GetAgentDomains([FromRoute] string agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var agent = await builder.GetAgentById(agentId);

            return new DomainPageViewModel { Domains = agent.Domains, Total = agent.Domains.Count };
        }
    }
}
