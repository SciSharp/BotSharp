using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using CSRedis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core
{
    public class AgentStorageInRedis<TAgent> : IAgentStorage<TAgent> 
        where TAgent : AgentBase
    {
        private static CSRedisClient csredis;
        private static string prefix = String.Empty;

        public AgentStorageInRedis()
        {
            if (csredis == null)
            {
                IConfiguration config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
                var db = config.GetSection("Database:Default").Value;
                var dbConnStr = config.GetSection($"Database:ConnectionStrings:{db}").Value;

                prefix = dbConnStr.Split(',').First(x => x.StartsWith("prefix=")).Split('=')[1];

                csredis = new CSRedisClient(dbConnStr);
            }
        }

        public async Task<TAgent> FetchById(string agentId)
        {
            var key = agentId;
            if (csredis.Exists(key))
            {
                return JsonConvert.DeserializeObject<TAgent>(csredis.Get(key));
            }
            else
            {
                return null;
            }
        }

        public async Task<TAgent> FetchByName(string agentName)
        {
            var agents = new List<TAgent>();

            var keys = csredis.Keys($"{prefix}*");
            foreach (string key in keys)
            {
                var data = csredis.Get(key.Substring(prefix.Length));
                var agent = JsonConvert.DeserializeObject<TAgent>(data);
                
                if(agent.Name == agentName)
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
            });

            csredis.Set(agent.Id, json);

            return true;
        }

        public async Task<int> PurgeAllAgents()
        {
            var keys = csredis.Keys($"{prefix}*");

            csredis.Remove(keys.Select(x => x.Substring(prefix.Length)).ToArray());

            return keys.Count();
        }

        public async Task<List<TAgent>> Query()
        {
            var agents = new List<TAgent>();

            var keys = csredis.Keys($"{prefix}*");
            foreach (string key in keys)
            {
                var data = csredis.Get(key.Substring(prefix.Length));
                var agent = JsonConvert.DeserializeObject<TAgent>(data);
                agents.Add(agent);
            }

            return agents;
        }
    }
}
