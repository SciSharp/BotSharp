using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using CSRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core
{
    public class AgentStorageInRedis<TAgent> : IAgentStorage<TAgent> 
        where TAgent : AgentBase
    {
        private static CSRedisClient csredis;

        public AgentStorageInRedis()
        {
            if (csredis == null)
            {
                csredis = new CSRedisClient("127.0.0.1:6379,defaultDatabase=botsharp,poolsize=50,ssl=false,writeBuffer=10240,prefix=agent.");
            }
        }

        public TAgent FetchById(string agentId)
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

        public TAgent FetchByName(string agentName)
        {
            var agents = new List<TAgent>();

            var keys = csredis.Keys("agent.*");
            foreach (string key in keys)
            {
                var data = csredis.Get(key.Split('.')[1]);
                var agent = JsonConvert.DeserializeObject<TAgent>(data);
                
                if(agent.Name == agentName)
                {
                    return agent;
                }
            }

            return default(TAgent);
        }

        public bool Persist(TAgent agent)
        {
            if (String.IsNullOrEmpty(agent.Id))
            {
                agent.Id = Guid.NewGuid().ToString();
            }

            csredis.Set(agent.Id, JsonConvert.SerializeObject(agent));

            return true;
        }

        public List<TAgent> Query()
        {
            var agents = new List<TAgent>();

            var keys = csredis.Keys("agent.*");
            foreach (string key in keys)
            {
                var data = csredis.Get(key.Split('.')[1]);
                var agent = JsonConvert.DeserializeObject<TAgent>(data);
                agents.Add(agent);
            }

            return agents;
        }
    }
}
