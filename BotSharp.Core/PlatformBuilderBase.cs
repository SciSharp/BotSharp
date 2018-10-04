using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TAgent> where TAgent : AgentBase
    {
        public IAgentStorage<TAgent> Storage { get; set; }

        public IConfiguration PlatformConfig { get; set; }


        public List<TAgent> GetAllAgents()
        {
            GetStorage();

            return Storage.Query();
        }

        public TAgent LoadAgentFromFile<TImporter>(string dataDir) where TImporter : IAgentImporter<TAgent>, new()
        {
            var meta = LoadMeta(dataDir);
            var importer = new TImporter();

            importer.AgentDir = dataDir;

            // Load agent summary
            var agent = importer.LoadAgent(meta);

            // Load user custom entities
            importer.LoadCustomEntities(agent);

            // Load agent intents
            importer.LoadIntents(agent);

            // Load system buildin entities
            importer.LoadBuildinEntities(agent);

            return agent;
        }

        private AgentImportHeader LoadMeta(string dataDir)
        {
            // load meta
            string metaJson = File.ReadAllText(Path.Combine(dataDir, "meta.json"));

            return JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);
        }

        public TAgent GetAgentById(string agentId)
        {
            GetStorage();

            return Storage.FetchById(agentId);
        }

        public TAgent GetAgentByName(string agentName)
        {
            GetStorage();

            return Storage.FetchByName(agentName);
        }

        public virtual bool SaveAgent(TAgent agent)
        {
            GetStorage();

            // default save agent in FileStorage
            Storage.Persist(agent);

            return true;
        }

        private IAgentStorage<TAgent> GetStorage()
        {
            if (Storage == null)
            {
                string storageName = PlatformConfig.GetValue<String>("AgentStorage");
                switch (storageName)
                {
                    case "AgentStorageInRedis":
                        Storage = Activator.CreateInstance<AgentStorageInRedis<TAgent>>();
                        break;
                    case "AgentStorageInMemory":
                        Storage = Activator.CreateInstance<AgentStorageInMemory<TAgent>>();
                        break;
                }
            }

            return Storage;
        }
    }
}
