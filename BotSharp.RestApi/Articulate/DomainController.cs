using BotSharp.Core.Engines.Articulate;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.RestApi.Articulate
{
#if ARTICULATE
    [Route("[controller]")]
    public class DomainController : ControllerBase
    {
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
        public void PostModel([FromBody] DomainModel domain)
        {
        }

        [HttpGet("/agent/{agentId}/domain")]
        public DomainPageViewModel GetAgentDomains([FromRoute] int agentId, [FromQuery] int start, [FromQuery] int limit)
        {
            var domains = new List<DomainModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => x.Contains($"agent-{agentId}-domain-")).ToList();
            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var domain = JsonConvert.DeserializeObject<DomainModel>(json);

                domains.Add(domain);
            }

            return new DomainPageViewModel { Domains = domains, Total = domains.Count };
        }
    }
#endif
}
