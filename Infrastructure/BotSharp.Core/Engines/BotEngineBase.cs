using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.Entities;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        protected Database Dc;

        protected AgentBase Agent { get; set; }

        public BotEngineBase()
        {
            Dc = new DefaultDataContextLoader().GetDefaultDc();
        }

        public Task<AiResponse> TextRequest(AiRequest request)
        {
            var preditor = new BotPredictor();
            var doc = preditor.Predict(Agent, new AiRequest
            {
                AgentDir = request.AgentDir,
                Model = request.Model,
                SessionId = request.SessionId,
                Text = request.Text
            }).Result;

            var parameters = new Dictionary<String, Object>();
            if(doc.Sentences[0].Entities == null)
            {
                doc.Sentences[0].Entities = new List<NlpEntity>();
            }
            doc.Sentences[0].Entities.ForEach(x => parameters[x.Entity] = x.Value);
            return Task.FromResult(new AiResponse
            {
                /*Lang = request.Language,
                Timestamp = DateTime.UtcNow,
                SessionId = request.SessionId,
                Status = new AIResponseStatus(),
                Result = new AIResponseResult
                {
                    Score = doc.Sentences[0].Intent == null ? 0 : doc.Sentences[0].Intent.Confidence,
                    ResolvedQuery = doc.Sentences[0].Text,
                    Fulfillment = new AIResponseFulfillment
                    {
                        Speech = agent.Intents.FirstOrDefault(tnt => tnt.Name == doc.Sentences[0].Intent?.Label)?.Responses?.Random()?.Messages?.Random()?.Speech
                    },
                    Parameters = parameters,
                    Entities = doc.Sentences[0].Entities,
                    Metadata = new AIResponseMetadata
                    {
                        IntentName = doc.Sentences[0].Intent?.Label
                    }
                }*/
            });
        }

        /*public TrainingCorpus GetIntentExpressions(AgentBase agent)
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
                        if (!trainingData.Entities.Any(y => y.EntityType == x.Alias && y.EntityValue == x.Text))
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
                        }
                    });

                    corpus.UserSays.Add(say);
                });
            });

            // remove Default Fallback Intent
            corpus.UserSays = corpus.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();

            return corpus;
        }*/

        public TrainingCorpus GetIntentExpressions()
        {
            TrainingCorpus corpus = new TrainingCorpus()
            {
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>(),
                Entities = new List<TrainingEntity>()
            };

            /*var expressParts = new List<IntentExpressionPart>();

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
                        if (!trainingData.Entities.Any(y => y.EntityType == x.Alias && y.EntityValue == x.Text))
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
                        }
                    });

                    corpus.UserSays.Add(say);
                });
            });

            // remove Default Fallback Intent
            corpus.UserSays = corpus.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();*/

            return corpus;
        }

        public virtual Task Train(BotTrainOptions options)
        {
            return Task.CompletedTask;
        }

    }
}
