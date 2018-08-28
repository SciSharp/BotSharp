using BotSharp.Core.Agents;
using BotSharp.Core.Engines.Rasa;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Bot engine/ platform base class
    /// </summary>
    public abstract class BotEngineBase
    {
        protected Database dc;

        public AIConfiguration AiConfig { get; set; }

        protected Agent agent { get; set; }

        public String DbInitializerPath { get; private set; }

        public BotEngineBase()
        {
            dc = new DefaultDataContextLoader().GetDefaultDc();
            string dataPath = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            DbInitializerPath = Path.Combine(dataPath, $"DbInitializer");
        }

        public AIResponse TextRequest(AIRequest request)
        {
            var preditor = new BotPredictor();
            var doc = preditor.Predict(agent, request).Result;
            var parameters = new Dictionary<String, Object>();
            doc.Sentences[0].Entities.ForEach(x => parameters.Add(x.Entity, x.Value));

            return new AIResponse
            {
                Lang = request.Language,
                Timestamp = DateTime.UtcNow,
                SessionId = request.SessionId,
                Status = new AIResponseStatus(),
                Result = new AIResponseResult
                {
                    Score = doc.Sentences[0].Intent == null ? 0 : doc.Sentences[0].Intent.Confidence,
                    ResolvedQuery = doc.Sentences[0].Text,
                    Fulfillment = new AIResponseFulfillment { },
                    Parameters = parameters,
                    Entities = doc.Sentences[0].Entities,
                    Metadata = new AIResponseMetadata
                    {
                        IntentName = doc.Sentences[0].Intent?.Label
                    }
                }
            };
        }

        public Agent LoadAgent(string id)
        {
            if (agent == null)
            {
                agent = dc.Table<Agent>()
                    .Include(x => x.Intents).ThenInclude(x => x.Contexts)
                    .Include(x => x.Entities).ThenInclude(x => x.Entries).ThenInclude(x => x.Synonyms)
                    .Include(x => x.MlConfig)
                    .FirstOrDefault(x => x.Id == id ||
                        x.ClientAccessToken == id ||
                        x.DeveloperAccessToken == id);
            }
            else
            {
                return agent;
            }

            return agent;
        }

        /// <summary>
        /// Restore a agent instance from backup json files
        /// </summary>
        /// <param name="importer"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public bool RestoreAgent<TAgentImporter>() where TAgentImporter : IAgentImporter, new()
        {
            string dataDir = Path.Combine(DbInitializerPath, "Agents");

            int row = dc.DbTran(() => {
                LoadAgentFromFile(dataDir);
                SaveAgent();
            });

            return row > 0;
        }

        public Agent LoadAgentFromFile(string dataDir)
        {
            var meta = LoadMeta(dataDir);
            IAgentImporter importer = null;

            switch (meta.Platform)
            {
                case "Dialogflow":
                    importer = new AgentImporterInDialogflow();
                    break;
                case "Rasa":
                    importer = new AgentImporterInRasa();
                    break;
                case "Sebis":
                    importer = new AgentImporterInSebis();
                    break;
                default:
                    break;
            }
            
            importer.AgentDir = dataDir;

            // Load agent summary
            agent = importer.LoadAgent(meta);

            // Load user custom entities
            importer.LoadCustomEntities(agent);

            // Load agent intents
            importer.LoadIntents(agent);

            // Load system buildin entities
            importer.LoadBuildinEntities(agent);

            // Generate corpus
            importer.AssembleTrainData(agent);

            return agent;
        }

        private AgentImportHeader LoadMeta(string dataDir)
        {
            // load meta
            string metaJson = File.ReadAllText(Path.Combine(dataDir, "meta.json"));

            return JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);
        }

        public String SaveAgent()
        {
            var existedAgent = dc.Table<Agent>().FirstOrDefault(x => x.Id == agent.Id || x.Name == agent.Name);
            if (existedAgent == null)
            {
                dc.Table<Agent>().Add(agent);
                return agent.Id;
            }
            else
            {
                agent.Id = existedAgent.Id;
                return existedAgent.Id;
            }
        }

        public TrainingCorpus GetIntentExpressions(Agent agent)
        {
            TrainingCorpus corpus = new TrainingCorpus()
            {
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>(),
                Entities = new List<TrainingEntity>()
            };

            var expressParts = new List<IntentExpressionPart>();

            var intents = agent.Intents;

            intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(exp =>
                {
                    exp.Data = exp.Data.OrderBy(x => x.UpdatedTime).ToList();

                    var say = new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.Select(x => x.Text)),
                        ContextHash = intent.ContextHash
                    };

                    // convert entity format
                    exp.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                    .ToList()
                    .ForEach(x =>
                    {
                        var part = new TrainingIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = $"{x.Meta}:{x.Alias}",
                            Start = x.Start
                        };

                        if (say.Entities == null) say.Entities = new List<TrainingIntentExpressionPart>();
                        say.Entities.Add(part);

                        // assemble entity synonmus
                        /*if (!trainingData.Entities.Any(y => y.EntityType == x.Alias && y.EntityValue == x.Text))
                        {
                            var allSynonyms = (from e in dc.Table<EntityType>()
                                               join ee in dc.Table<EntityEntry>() on e.Id equals ee.EntityId
                                               join ees in dc.Table<EntrySynonym>() on ee.Id equals ees.EntityEntryId
                                               where e.Name == x.Alias && ee.Value == x.Text & ees.Synonym != x.Text
                                               select ees.Synonym).ToList();

                            var te = new TrainingEntity
                            {
                                EntityType = $"{x.Meta}:{x.Alias}",
                                EntityValue = x.Text,
                                Synonyms = allSynonyms
                            };

                            trainingData.Entities.Add(te);
                        }*/
                    });

                    corpus.UserSays.Add(say);
                });
            });

            // remove Default Fallback Intent
            corpus.UserSays = corpus.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();

            return corpus;
        }

        public TrainingCorpus GetIntentExpressions()
        {
            TrainingCorpus corpus = new TrainingCorpus()
            {
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>(),
                Entities = new List<TrainingEntity>()
            };

            var expressParts = new List<IntentExpressionPart>();

            var intents = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.UserSays).ThenInclude(say => say.Data)
                .Where(x => x.AgentId == agent.Id && x.UserSays.Count > 0)
                .ToList();

            intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(exp =>
                {
                    exp.Data = exp.Data.OrderBy(x => x.UpdatedTime).ToList();

                    var say = new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.Select(x => x.Text)),
                        ContextHash = intent.ContextHash
                    };

                    // convert entity format
                    exp.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                    .ToList()
                    .ForEach(x =>
                    {
                        var part = new TrainingIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = $"{x.Meta}:{x.Alias}",
                            Start = x.Start
                        };

                        if (say.Entities == null) say.Entities = new List<TrainingIntentExpressionPart>();
                        say.Entities.Add(part);

                        // assemble entity synonmus
                        /*if (!trainingData.Entities.Any(y => y.EntityType == x.Alias && y.EntityValue == x.Text))
                        {
                            var allSynonyms = (from e in dc.Table<EntityType>()
                                               join ee in dc.Table<EntityEntry>() on e.Id equals ee.EntityId
                                               join ees in dc.Table<EntrySynonym>() on ee.Id equals ees.EntityEntryId
                                               where e.Name == x.Alias && ee.Value == x.Text & ees.Synonym != x.Text
                                               select ees.Synonym).ToList();

                            var te = new TrainingEntity
                            {
                                EntityType = $"{x.Meta}:{x.Alias}",
                                EntityValue = x.Text,
                                Synonyms = allSynonyms
                            };

                            trainingData.Entities.Add(te);
                        }*/
                    });

                    corpus.UserSays.Add(say);
                });
            });

            // remove Default Fallback Intent
            corpus.UserSays = corpus.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();

            return corpus;
        }

        public virtual Task Train(BotTrainOptions options)
        {
            return Task.CompletedTask;
        }

    }
}
