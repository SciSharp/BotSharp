using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core;
using BotSharp.Platform.Abstractions;
using BotSharp.Platform.Models;
using DotNetToolkit;
using BotSharp.Platform.Dialogflow.Models;
using System.IO;
using Microsoft.Extensions.Configuration;
using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.AiRequest;
using Turing.NET;
using System.Text.RegularExpressions;
using BotSharp.Platform.Models.Contexts;

namespace BotSharp.Platform.Dialogflow
{
    public class DialogflowAi<TAgent> :
        PlatformBuilderBase<TAgent>,
        IPlatformBuilder<TAgent>
        where TAgent : AgentModel
    {
        IConfiguration config;

        public DialogflowAi(IAgentStorageFactory<TAgent> agentStorageFactory, IContextStorageFactory<AIContext> contextStorageFactory, IPlatformSettings settings, IConfiguration config)
            : base(agentStorageFactory, contextStorageFactory, settings)
        {
            this.config = config;
        }

        public async Task<TrainingCorpus> ExtractorCorpus(TAgent agent)
        {
            var corpus = new TrainingCorpus
            {
                Entities = new List<TrainingEntity>(),
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>()
            };

            agent.Entities.ForEach(entity =>
            {
                corpus.Entities.Add(new TrainingEntity
                {
                    Entity = entity.Name,
                    Values = entity.Entries.Select(x => new TrainingEntitySynonym
                    {
                        Value = x.Value,
                        Synonyms = x.Synonyms.Select(y => y.Synonym).ToList()
                    }).ToList()
                });
            });

            agent.Intents.ForEach(intent =>
            {
                // filter unexpected intents
                if(intent.Name != "Default Fallback Intent")
                {
                    // caculate contexts hash
                    intent.ContextHash = String.Join('_', intent.Contexts.OrderBy(x => x.Name).Select(x => x.Name)).GetMd5Hash();

                    intent.UserSays.ForEach(say => {
                        corpus.UserSays.Add(new TrainingIntentExpression<TrainingIntentExpressionPart>
                        {
                            Intent = intent.Name,
                            Text = String.Join("", say.Data.Select(x => x.Text)),
                            Entities = say.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                            .Select(x => new TrainingIntentExpressionPart
                            {
                                Value = x.Text,
                                Entity = x.Meta,
                                Start = x.Start
                            })
                            .ToList(),
                            ContextHash = intent.ContextHash
                        });
                    });
                }
            });

            
            return corpus;
        }

        public override async Task<TextClassificationResult> FallbackResponse(AiRequest request)
        {
            if (config.GetValue<bool>("overrideFallback"))
            {
                var turing = new TuringAgent(config);
                var tulingResponse = turing.Request(new TuringRequest
                {
                    Perception = new TuringRequestPerception
                    {
                        InputText = new TuringInputText { Text = request.Text }
                    }
                });

                var result = tulingResponse.Results.FirstOrDefault(x => x.ResultType == "text").Values.Text;

                return new TextClassificationResult
                {
                    Classifier = "turing",
                    Text = tulingResponse.Results.FirstOrDefault(x => x.ResultType == "text").Values.Text
                };
            }
            else
            {
                return await base.FallbackResponse(request);
            }
        }

        public override async Task<TResult> AssembleResult<TResult>(AiRequest request, AiResponse response)
        {
            var intent = Agent.Intents.Find(x => x.Name == response.Intent);
            var presetResponse = intent.Responses.FirstOrDefault();

            // format messages
            presetResponse.Messages = presetResponse.Messages.Where(x => x.Speech.Length > 0).ToList();
            if (presetResponse.Messages.Count == 0)
            {
                presetResponse.Messages.Add(new IntentResponseMessage
                {
                    Speech = "\"" + intent.Name + "\""
                });
            }

            // fill parameters
            presetResponse.Parameters.ForEach(p =>
            {
                var entity = response.Entities.FirstOrDefault(x => x.Entity == p.DataType);
                p.Value = entity?.Value;
            });

            var matches = Regex.Matches(presetResponse.Messages.Random().Speech, "\".*?\"").Cast<Match>();
            var speech = matches.Count() == 0 ? String.Empty : matches.ToList().Random().Value;

            var contexts = HandleContexts(request.SessionId, presetResponse);

            var aiResponse = new AIResponseResult
            {
                ResolvedQuery = response.ResolvedQuery,
                Action = presetResponse.Action,
                Metadata = new AIResponseMetadata
                {
                    IntentName = response.Intent
                },
                Intent = response.Intent,
                Fulfillment = new AIResponseFulfillment
                {
                    Messages = presetResponse.Messages.ToList<object>(),
                    Speech = speech.Length > 1 ? speech.Substring(1, speech.Length - 2) : String.Empty
                },
                Score = response.Score,
                Source = response.Source,
                Contexts = contexts.ToArray(),
                Parameters = presetResponse.Parameters.Where(x => !String.IsNullOrEmpty(x.Value)).ToDictionary(item => item.Name, item => (object)item.Value)
            };

            return (TResult)(object)aiResponse;
        }

        private List<AIContext> HandleContexts(string sessionId, IntentResponse response)
        {
            var newContexts = response.Contexts.Select(x => new AIContext
            {
                Name = x.Name,
                Lifespan = x.Lifespan,
                Parameters = response.Parameters.Select(p => new KeyValuePair<string, object>(p.Name, p.Value)).ToDictionary(d => d.Key, d => d.Value == null ? String.Empty : d.Value)
            }).ToList();

            // persist
            var ctxStore = contextStorageFactory.Get();
            ctxStore.Persist(sessionId, newContexts.ToArray());

            return newContexts;
        }
    }
}
