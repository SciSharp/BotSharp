using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using BotSharp.Core.Conversations;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Engines
{
    public static class RasaRequestExtension
    {
        public static AIResponse TextRequest(this RasaAi rasa, string text, RequestExtras requestExtras)
        {
            return rasa.TextRequest(new AIRequest(text, requestExtras));
        }

        public static IntentResponse HandleIntentPerContextIn(Agent agent, AIConfiguration aiConfig, AIRequest request, RasaResponse response, Database dc)
        {
            // Merge input contexts
            var contexts = dc.Table<ConversationContext>()
                .Where(x => x.ConversationId == aiConfig.SessionId && x.Lifespan > 0)
                .ToList()
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToList();

            contexts.AddRange(request.Contexts.Select(x => new AIContext { Name = x.Name.ToLower(), Lifespan = x.Lifespan }));
            contexts = contexts.OrderBy(x => x.Name).ToList();

            // search all potential intents which input context included in contexts
            var intents = agent.Intents.Where(it =>
            {
                if (contexts.Count == 0)
                {
                    return it.Contexts.Count() == 0;
                }
                else
                {
                    return it.Contexts.Count() == 0 ||
                        it.Contexts.Count(x => contexts.Select(ctx => ctx.Name).Contains(x.Name.ToLower())) == it.Contexts.Count;
                }
            }).OrderByDescending(x => x.Contexts.Count).ToList();

            if (response.IntentRanking == null)
            {
                response.IntentRanking = new List<RasaResponseIntent>
                {
                    response.Intent
                };
            }

            response.IntentRanking = response.IntentRanking.Where(x => x.Confidence > agent.MlConfig.MinConfidence).ToList();
            response.IntentRanking = response.IntentRanking.Where(x => intents.Select(i => i.Name).Contains(x.Name)).ToList();

            // add Default Fallback Intent 
            if (response.IntentRanking.Count == 0)
            {
                var defaultFallbackIntent = agent.Intents.FirstOrDefault(x => x.Name == "Default Fallback Intent");
                response.IntentRanking.Add(new RasaResponseIntent
                {
                    Name = defaultFallbackIntent.Name,
                    Confidence = decimal.Parse("0.8")
                });
            }

            response.Intent = response.IntentRanking.First();

            var intent = (dc.Table<Intent>().Where(x => x.AgentId == agent.Id && x.Name == response.Intent.Name)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters).ThenInclude(x => x.Prompts)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)).First();

            var intentResponse = ArrayHelper.GetRandom(intent.Responses);
            intentResponse.IntentName = intent.Name;

            return intentResponse;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="intentResponse"></param>
        /// <param name="response"></param>
        /// <param name="request"></param>
        /// <returns>Required field is missed</returns>
        public static void HandleParameter(Agent agent, IntentResponse intentResponse, RasaResponse response, AIRequest request)
        {
            if (intentResponse == null) return;

            intentResponse.Parameters.ForEach(p => {
                string query = request.Query.First();
                var entity = response.Entities.FirstOrDefault(x => x.Entity == p.Name || x.Entity.Split(':').Contains(p.Name));
                if (entity != null)
                {
                    p.Value = query.Substring(entity.Start, entity.End - entity.Start);
                }

                // convert to Standard entity value
                if (!String.IsNullOrEmpty(p.Value) && !p.DataType.StartsWith("sys."))
                {
                    p.Value = agent.Entities
                        .FirstOrDefault(x => x.Name == p.DataType)
                        .Entries
                        .FirstOrDefault((entry) =>
                        {
                            return entry.Value.ToLower() == p.Value.ToLower() ||
                                entry.Synonyms.Select(synonym => synonym.Synonym.ToLower()).Contains(p.Value.ToLower());
                        })?.Value;
                }

                // fixed entity per request
                if (request.Entities != null)
                {
                    var fixedEntity = request.Entities.FirstOrDefault(x => x.Name == p.Name);
                    if (fixedEntity != null)
                    {
                        if (query.ToLower().Contains(fixedEntity.Entries.First().Value.ToLower()))
                        {
                            p.Value = fixedEntity.Entries.First().Value;
                        }
                    }
                }
            });
        }

        public static void HandleMessage(IntentResponse intentResponse)
        {
            if (intentResponse == null) return;

            var missingRequiredParameter = intentResponse.Parameters.FirstOrDefault(x => x.Required && String.IsNullOrEmpty(x.Value));
            if (missingRequiredParameter != null)
            {
                intentResponse.Messages = new List<IntentResponseMessage> {
                    new IntentResponseMessage {
                        Type = AIResponseMessageType.Text,
                        Speech = ArrayHelper.GetRandom(missingRequiredParameter.Prompts).Prompt,
                        IntentResponseId = intentResponse.Id,
                        UpdatedTime = DateTime.UtcNow
                    }
                };
            }
            else
            {
                intentResponse.Messages = intentResponse.Messages.OrderBy(x => x.UpdatedTime).ToList();
            }
            
            intentResponse.Messages.ToList()
                .ForEach(msg =>
                {
                    if (msg.Type == AIResponseMessageType.Custom)
                    {

                    }
                    else
                    {
                        if (msg.Speech != "[]")
                        {
                            msg.Speech = msg.Speech.StartsWith("[") ?
                            ArrayHelper.GetRandom(msg.Speech.Substring(2, msg.Speech.Length - 4).Split(new string[] { "\",\"" }, StringSplitOptions.None).ToList()) :
                            msg.Speech;

                            msg.Speech = ReplaceParameters4Response(intentResponse.Parameters, msg.Speech);
                        }
                    }
                });
        }

        private static string ReplaceParameters4Response(List<IntentResponseParameter> parameters, string text)
        {
            var reg = new Regex(@"\$\w+");

            reg.Matches(text).Cast<Match>().ToList().ForEach(token => {
                var parameter = parameters.FirstOrDefault(x => x.Name == token.Value.Substring(1));
                if(parameter != null)
                {
                    text = text.Replace(token.Value, parameter?.Value?.ToString());
                }
            });

            return text;
        }

        public static void HandleContext(Database dc, AIConfiguration AiConfig, IntentResponse intentResponse, AIResponse aiResponse)
        {
            if (intentResponse == null) return;

            // Merge context lifespan
            // override if exists, otherwise add, delete if lifespan is zero
            dc.DbTran(() =>
            {
                var sessionContexts = dc.Table<ConversationContext>().Where(x => x.ConversationId == AiConfig.SessionId).ToList();

                // minus 1 round
                sessionContexts.Where(x => !intentResponse.Contexts.Select(ctx => ctx.Name).Contains(x.Context))
                    .ToList()
                    .ForEach(ctx => ctx.Lifespan = ctx.Lifespan - 1);

                intentResponse.Contexts.ForEach(ctx =>
                {
                    var session1 = sessionContexts.FirstOrDefault(x => x.Context == ctx.Name);

                    if (session1 != null)
                    {
                        if (ctx.Lifespan == 0)
                        {
                            dc.Table<ConversationContext>().Remove(session1);
                        }
                        else
                        {
                            session1.Lifespan = ctx.Lifespan;
                        }
                    }
                    else
                    {
                        dc.Table<ConversationContext>().Add(new ConversationContext
                        {
                            ConversationId = AiConfig.SessionId,
                            Context = ctx.Name,
                            Lifespan = ctx.Lifespan
                        });
                    }
                });
            });

            aiResponse.Result.Contexts = dc.Table<ConversationContext>()
                .Where(x => x.Lifespan > 0 && x.ConversationId == AiConfig.SessionId)
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToArray();
        }

        public static string GetModelPerContexts(Agent agent, AIConfiguration aiConfig, AIRequest request, Database dc)
        {
            // Merge input contexts
            var contexts = dc.Table<ConversationContext>()
                .Where(x => x.ConversationId == aiConfig.SessionId && x.Lifespan > 0)
                .ToList()
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToList();

            contexts.AddRange(request.Contexts.Select(x => new AIContext { Name = x.Name.ToLower(), Lifespan = x.Lifespan }));
            contexts = contexts.OrderBy(x => x.Name).ToList();

            // search all potential intents which input context included in contexts
            var intents = agent.Intents.Where(it =>
            {
                if (contexts.Count == 0)
                {
                    return it.Contexts.Count() == 0;
                }
                else
                {
                    return it.Contexts.Count() > 0 &&
                        it.Contexts.Count(x => contexts.Select(ctx => ctx.Name).Contains(x.Name.ToLower())) == it.Contexts.Count;
                }
            }).OrderByDescending(x => x.Contexts.Count).ToList();

            // query per request contexts
            var contextHashs = intents.Select(x => x.ContextHash).Distinct().ToList();

            return contextHashs.FirstOrDefault();
        }
    }
}
