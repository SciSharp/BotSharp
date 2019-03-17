using BotSharp.Core.Engines;
using BotSharp.Platform.Abstractions;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.Contexts;
using BotSharp.Platform.Models.Entities;
using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Models.MachineLearning;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core
{
    public abstract class PlatformBuilderBase<TAgent> where TAgent : AgentBase
    {
        public TAgent Agent { get; set; }

        public IAgentStorage<TAgent> Storage { get; set; }

        protected readonly IAgentStorageFactory<TAgent> agentStorageFactory;
        protected readonly IContextStorageFactory<AIContext> contextStorageFactory;
        protected readonly IPlatformSettings settings;

        public PlatformBuilderBase(IAgentStorageFactory<TAgent> agentStorageFactory, IContextStorageFactory<AIContext> contextStorageFactory, IPlatformSettings settings)
        {
            this.agentStorageFactory = agentStorageFactory;
            this.contextStorageFactory = contextStorageFactory;
            this.settings = settings;
            GetAgentStorage();
        }

        public async Task<List<TAgent>> GetAllAgents()
        {
            return await Storage.Query();
        }

        public async Task<TAgent> LoadAgentFromFile<TImporter>(string dataDir) where TImporter : IAgentImporter<TAgent>, new()
        {
            Console.WriteLine($"Loading agent from folder {dataDir}");

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

            Console.WriteLine($"Loaded agent: {agent.Name} {agent.Id}");

            Agent = agent;

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
            return await Storage.FetchById(agentId);
        }

        public async Task<TAgent> GetAgentByName(string agentName)
        {
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

            ModelMetaData meta = null;

            // train by contexts
            corpus.UserSays.GroupBy(x => x.ContextHash).Select(g => new
            {
                Context = g.Key,
                Corpus = new TrainingCorpus
                {
                    Entities = corpus.Entities,
                    UserSays = corpus.UserSays.Where(x => x.ContextHash == g.Key).ToList()
                }
            })
            .ToList()
            .ForEach(async c =>
            {
                var trainer = new BotTrainer(settings);
                agent.Corpus = c.Corpus;

                meta = await trainer.Train(agent, new BotTrainOptions
                {
                    AgentDir = options.AgentDir,
                    Model = options.Model + $"{Path.DirectorySeparatorChar}{c.Context}"
                });
            });

            meta.Pipeline.Clear();
            meta.Model = options.Model;

            return meta;
        }

        public virtual async Task<TResult> TextRequest<TResult>(AiRequest request)
        {
            // merge last contexts
            string contextHash = await GetContextsHash(request);

            Console.WriteLine($"TextRequest: {request.Text}, {request.AgentId}, {string.Join(",", request.Contexts)}, {request.SessionId}");

            // Load agent
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", request.AgentId);
            var model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            var modelPath = Path.Combine(projectPath, model);
            request.AgentDir = projectPath;
            request.Model = model + $"{Path.DirectorySeparatorChar}{contextHash}";

            Agent = await GetAgentById(request.AgentId);

            var preditor = new BotPredictor();
            var doc = await preditor.Predict(Agent, request);

            var predictedIntent = doc.Sentences[0].Intent;

            if (predictedIntent.Confidence < Agent.MlConfig.MinConfidence)
            {
                predictedIntent = await FallbackResponse(request);

                predictedIntent.Confidence = Agent.MlConfig.MinConfidence;
                predictedIntent.Label = "fallback";

                Agent.Intents.Add(new Intent
                {
                    Name = predictedIntent.Label,
                    Responses = new List<IntentResponse>
                        {
                            new IntentResponse
                            {
                                IntentName = predictedIntent.Label,
                                Messages = new List<IntentResponseMessage>
                                {
                                    new IntentResponseMessage
                                    {
                                        Speech = "\"" + predictedIntent.Text + "\"",
                                        Type = AIResponseMessageType.Text
                                    }
                                }
                            }
                        }
                });
            }

            var aiResponse = new AiResponse
            {
                ResolvedQuery = request.Text,
                Score = predictedIntent.Confidence,
                Source = predictedIntent.Classifier,
                Intent = predictedIntent.Label,
                Entities = doc.Sentences[0].Entities
            };

            Console.WriteLine($"TextResponse: {aiResponse.Intent}, {request.SessionId}");

            return await AssembleResult<TResult>(request, aiResponse);
        }

        private async Task<string> GetContextsHash(AiRequest request)
        {
            var ctxStore = contextStorageFactory.Get();
            var contexts = await ctxStore.Fetch(request.SessionId);
            for(int i = 0; i < contexts.Length; i++)
            {
                var ctx = contexts[i];
                if (ctx.Lifespan > 0 && !request.Contexts.Exists(x => x == ctx.Name))
                {
                    request.Contexts.Add(ctx.Name);
                }
            }

            request.Contexts = request.Contexts.OrderBy(x => x).ToList();

            return String.Join("_", request.Contexts).GetMd5Hash();
        }

        public virtual async Task<TextClassificationResult> FallbackResponse(AiRequest request)
        {
            throw new NotImplementedException("FallbackResponse");
        }

        public virtual async Task<TResult> AssembleResult<TResult>(AiRequest request, AiResponse response)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> SaveAgent(TAgent agent)
        {
            // default save agent in FileStorage
            await Storage.Persist(agent);

            return true;
        }
        protected IAgentStorage<TAgent> GetAgentStorage()
        {
            if (Storage == null)
            {
                Storage = agentStorageFactory.Get();
            }

            return Storage;
        }
    }
}
