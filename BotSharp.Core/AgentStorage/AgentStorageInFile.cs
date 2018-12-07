using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using CSRedis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.AgentStorage
{
    public class AgentStorageInFile<TAgent> : IAgentStorage<TAgent> 
        where TAgent : AgentBase
    {
        private static string storageDir;

        public AgentStorageInFile()
        {
            IConfiguration config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var db = config.GetSection("Database:Default").Value;
            storageDir = config.GetSection($"Database:ConnectionStrings:{db}").Value;
            string contentDir = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            storageDir = storageDir.Replace("|DataDirectory|", contentDir + Path.DirectorySeparatorChar + "AgentStorage" + Path.DirectorySeparatorChar);

            if (!Directory.Exists(storageDir))
            {
                Directory.CreateDirectory(storageDir);
            }
        }

        public async Task<TAgent> FetchById(string agentId)
        {
            string dataPath = Path.Combine(storageDir, agentId + ".json");
            if (File.Exists(dataPath))
            {
                string json = File.ReadAllText(dataPath);
                return JsonConvert.DeserializeObject<TAgent>(json);
            }
            else
            {
                return default(TAgent);
            }
        }

        public async Task<TAgent> FetchByName(string agentName)
        {
            var files = Directory.GetFiles(storageDir);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                string json = File.ReadAllText(file);
                var agent = JsonConvert.DeserializeObject<TAgent>(json);
                if (agent.Name.ToLower() == agentName.ToLower())
                {
                    return agent;
                }
            }

            return default(TAgent);
        }

        public async Task<bool> Persist(TAgent agent)
        {
            if (String.IsNullOrEmpty(agent.Id))
            {
                agent.Id = Guid.NewGuid().ToString();
            }

            var json = JsonConvert.SerializeObject(agent, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                }
            });

            string dataPath = Path.Combine(storageDir, agent.Id + ".json");

            File.WriteAllText(dataPath, json);

            return true;
        }

        public async Task<int> PurgeAllAgents()
        {
            var files = Directory.GetFiles(storageDir);
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }

            return files.Length;
        }

        public async Task<List<TAgent>> Query()
        {
            var agents = new List<TAgent>();

            var files = Directory.GetFiles(storageDir);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                string json = File.ReadAllText(file);
                var agent = JsonConvert.DeserializeObject<TAgent>(json);
                agents.Add(agent);
            }

            return agents;
        }
    }
}
