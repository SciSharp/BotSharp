using BotSharp.Core.Engines;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.MachineLearning;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TAgent> where TAgent : AgentBase
    {
        public IAgentStorage<TAgent> Storage { get; set; }

        private readonly IAgentStorageFactory<TAgent> agentStorageFactory;

        public PlatformBuilderBase(IAgentStorageFactory<TAgent> agentStorageFactory)
        {
            this.agentStorageFactory = agentStorageFactory;
        }

        public async Task<List<TAgent>> GetAllAgents()
        {
            await GetStorage();

            return await Storage.Query();
        }

        public async Task<TAgent> LoadAgentFromFile<TImporter>(string dataDir) where TImporter : IAgentImporter<TAgent>, new()
        {
            var meta = LoadMeta(dataDir);
            var importer = new TImporter
            {
                AgentDir = dataDir
            };

            // Load agent summary
            var agent = await importer.LoadAgent(meta);

            // Load user custom entities
            await importer.LoadCustomEntities(agent);

            // Load agent intents
            await importer.LoadIntents(agent);

            // Load system buildin entities
            await importer.LoadBuildinEntities(agent);

            return agent;
        }

        private AgentImportHeader LoadMeta(string dataDir)
        {
            // load meta
            string metaJson = File.ReadAllText(Path.Combine(dataDir, "meta.json"));

            return JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);
        }

        public async Task<TAgent> GetAgentById(string agentId)
        {
            GetStorage();

            return await Storage.FetchById(agentId);
        }

        public async Task<TAgent> GetAgentByName(string agentName)
        {
            await GetStorage();

            return await Storage.FetchByName(agentName);
        }

        public virtual async Task<ModelMetaData> Train(TAgent agent, TrainingCorpus corpus, BotTrainOptions options)
        {
            if (String.IsNullOrEmpty(options.AgentDir))
            {
                options.AgentDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id);
            }

            if (String.IsNullOrEmpty(options.Model))
            {
                options.Model = "model_" + DateTime.UtcNow.ToString("yyyyMMdd");
            }

            var trainer = new BotTrainer();
            agent.Corpus = corpus;

            var info = await trainer.Train(agent, options);

            return info;
        }

        public virtual async Task<bool> SaveAgent(TAgent agent)
        {
            await GetStorage();

            // default save agent in FileStorage
            await Storage.Persist(agent);

            return true;
        }

        protected async Task<IAgentStorage<TAgent>> GetStorage()
        {
            if (Storage == null)
            {
                Storage = await agentStorageFactory.Get();
            }
            return Storage;
        }
    }
}
