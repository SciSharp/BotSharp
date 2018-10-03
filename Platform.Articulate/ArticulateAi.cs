using BotSharp.Core;
using BotSharp.Core.Engines;
using BotSharp.Models.NLP;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using DotNetToolkit;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Articulate
{
    /// <summary>
    /// A platform for building conversational interfaces with intelligent agents (chatbots) 
    /// http://spg.ai/projects/articulate
    /// This implementation takes over APIs of Articulate's 7500 port.
    /// </summary>
    public class ArticulateAi<TAgent> : 
        PlatformBuilderBase<TAgent>, 
        IPlatformBuilder<TAgent> 
        where TAgent : AgentModel
    {
        public DialogRequestOptions RequestOptions { get; set; }

        public Tuple<TAgent, DomainModel> GetAgentByDomainId(String domainId)
        {
            var results = GetAllAgents();

            foreach (TAgent agent in results)
            {
                var domain = agent.Domains.FirstOrDefault(x => x.Id == domainId);

                if (domain != null)
                {
                    return new Tuple<TAgent, DomainModel>(agent, domain);
                }
            }

            return null;
        }

        public Tuple<TAgent, DomainModel, IntentModel> GetAgentByIntentId(String intentId)
        {
            var results = GetAllAgents();

            foreach (TAgent agent in results)
            {
                foreach (DomainModel domain in agent.Domains)
                {
                    var intent = domain.Intents.FirstOrDefault(x => x.Id == intentId);
                    if (intent != null)
                    {
                        return new Tuple<TAgent, DomainModel, IntentModel>(agent, domain, intent);
                    }
                }
            }

            return null;
        }

        public List<IntentModel> GetReferencedIntentsByEntity(string entityId)
        {
            var intents = new List<IntentModel>();
            var allAgents = GetAllAgents();
            foreach (TAgent agent in allAgents)
            {
                foreach (DomainModel domain in agent.Domains)
                {
                    foreach (IntentModel intent in domain.Intents)
                    {
                        if(intent.Examples.Exists(x => x.Entities.Exists(y => y.EntityId == entityId)))
                        {
                            intents.Add(intent);
                        }
                    }
                }
            }

            return intents;
        }

        public TrainingCorpus ExtractorCorpus(TAgent agent)
        {
            var corpus = new TrainingCorpus();
            corpus.Entities = agent.Entities.Select(x => new TrainingEntity
            {
                Entity = x.EntityName,
                Values = x.Examples.Select(y => new TrainingEntitySynonym
                {
                    Value = y.Value,
                    Synonyms = y.Synonyms
                }).ToList()
            }).ToList();

            corpus.UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>();

            foreach(DomainModel domain in agent.Domains)
            {
                foreach(IntentModel intent in domain.Intents)
                {
                    foreach(IntentExampleModel example in intent.Examples)
                    {
                        var say = new TrainingIntentExpression<TrainingIntentExpressionPart>()
                        {
                            Intent = intent.IntentName,
                            Text = example.UserSays,
                            Entities = example.Entities.Select(x => new TrainingIntentExpressionPart
                            {
                                Entity = x.Entity,
                                Start = x.Start,
                                Value = x.Value
                            }).ToList()
                        };

                        corpus.UserSays.Add(say);
                    }
                }
            }

            return corpus;
        }

        public override bool SaveAgent(TAgent agent)
        {
            agent.Status = "Changed";
            agent.LastTraining = DateTime.UtcNow;
            return base.SaveAgent(agent);
        }

        public async Task<bool> Train(TAgent agent, TrainingCorpus corpus)
        {
            string agentDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id);
            var model = "model_" + DateTime.UtcNow.ToString("yyyyMMdd");

            var trainer = new BotTrainer();
            agent.Corpus = corpus;

            var trainOptions = new BotTrainOptions
            {
                AgentDir = agentDir,
                Model = model
            };

            var info = await trainer.Train(agent, trainOptions);

            return true;
        }

        public AiResponse TextRequest(AiRequest request)
        {
            var aiResponse = new AiResponse();

            // Load agent
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", request.AgentId);
            var model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            var modelPath = Path.Combine(projectPath, model);
            request.AgentDir = projectPath;
            request.Model = model;

            var agent = GetAgentById(request.AgentId);

            var preditor = new BotPredictor();
            var doc = preditor.Predict(agent, request).Result;

            var parameters = new Dictionary<String, Object>();
            if (doc.Sentences[0].Entities == null)
            {
                doc.Sentences[0].Entities = new List<NlpEntity>();
            }
            doc.Sentences[0].Entities.ForEach(x => parameters[x.Entity] = x.Value);

            aiResponse.Intent = doc.Sentences[0].Intent.Label;
            aiResponse.Speech = aiResponse.Intent;

            return aiResponse;
        }
    }
}
