using BotSharp.Core.Engines.Articulate;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
    public class AgentController : ControllerBase
    {
        [HttpGet]
        public List<AgentModel> GetAgent()
        {
            var agents = new List<AgentModel>();

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => Regex.IsMatch(x, @"agent-\d+.json")).ToList();
            for (int i = 0; i< agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var agent = JsonConvert.DeserializeObject<AgentModel>(json);

                agents.Add(agent);
            }

            return agents;
        }

        [HttpGet("{agentId}")]
        public AgentModel GetAgent([FromRoute] int agentId)
        {
            string dataPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate", $"agent-{agentId}.json");

            string json = System.IO.File.ReadAllText(dataPath);

            var agent = JsonConvert.DeserializeObject<AgentModel>(json);

            return agent;
        }

        [HttpGet("name/{agentName}")]
        public AgentModel GetAgent([FromRoute] string agentName)
        {
            AgentModel agent = null;

            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate");

            var agentPaths = Directory.GetFiles(dataDir).Where(x => Regex.IsMatch(x, @"agent-\d+\.json")).ToList();

            for (int i = 0; i < agentPaths.Count; i++)
            {
                string json = System.IO.File.ReadAllText(agentPaths[i]);

                var agentTmp = JsonConvert.DeserializeObject<AgentModel>(json);

                if(agentTmp.AgentName == agentName)
                {
                    agent = agentTmp;
                }
            }

            return agent;
        }
    }
#endif
}
