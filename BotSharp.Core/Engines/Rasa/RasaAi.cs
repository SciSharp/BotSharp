using BotSharp.Core.Adapters.Rasa;
using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Rasa nlu >= 0.12
    /// </summary>
    public class RasaAi : BotEngineBase, IBotPlatform
    {
        public AIResponse TextRequest(AIRequest request)
        {
            AIResponse aiResponse = new AIResponse();

            string model = RasaRequestExtension.GetModelPerContexts(agent, AiConfig, request, dc);
            var result = CallRasa(agent.Id, request.Query.First(), model);

            result.Content.Log();

            RasaResponse response = result.Data;
            aiResponse.Id = Guid.NewGuid().ToString();
            aiResponse.Lang = agent.Language;
            aiResponse.Status = new AIResponseStatus { };
            aiResponse.SessionId = AiConfig.SessionId;
            aiResponse.Timestamp = DateTime.UtcNow;

            var intentResponse = RasaRequestExtension.HandleIntentPerContextIn(agent, AiConfig, request, result.Data, dc);

            RasaRequestExtension.HandleParameter(agent, intentResponse, response, request);

            RasaRequestExtension.HandleMessage(intentResponse);

            aiResponse.Result = new AIResponseResult
            {
                Source = "agent",
                ResolvedQuery = request.Query.First(),
                Action = intentResponse?.Action,
                Parameters = intentResponse?.Parameters?.ToDictionary(x => x.Name, x => (object)x.Value),
                Score = response.Intent.Confidence,
                Metadata = new AIResponseMetadata { IntentId = intentResponse?.IntentId, IntentName = intentResponse?.IntentName },
                Fulfillment = new AIResponseFulfillment
                {
                    Messages = intentResponse?.Messages?.Select(x => {
                        if (x.Type == AIResponseMessageType.Custom)
                        {
                            return (new
                            {
                                x.Type,
                                Payload = JsonConvert.DeserializeObject(x.PayloadJson)
                            }) as Object;
                        }
                        else
                        {
                            return (new { x.Type, x.Speech }) as Object;
                        }

                    }).ToList()
                }
            };

            RasaRequestExtension.HandleContext(dc, AiConfig, intentResponse, aiResponse);

            Console.WriteLine(JsonConvert.SerializeObject(aiResponse.Result));

            return aiResponse;
        }

        private IRestResponse<RasaResponse> CallRasa(string projectId, string text, string model)
        {
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var client = new RestClient($"{config.GetSection("RasaNlu:url").Value}");

            var rest = new RestRequest("parse", Method.POST);
            string json = JsonConvert.SerializeObject(new { Project = projectId, Q = text, Model = model },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            rest.AddParameter("application/json", json, ParameterType.RequestBody);

            return client.Execute<RasaResponse>(rest);
        }

        public void Train()
        {
            var trainingData = new RasaTrainingData
            {
                Entities = new List<RasaTraningEntity>(),
                UserSays = new List<RasaIntentExpression>()
            };

            var corpus = GetIntentExpressions();
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var client = new RestClient($"{config.GetSection("RasaNlu:url").Value}");

            var contextHashs = corpus.UserSays
                .Select(x => x.ContextHash)
                .Distinct()
                .ToList();

            contextHashs.ForEach(ctx =>
            {
                var common_examples = corpus.UserSays.Where(x => x.ContextHash == ctx || x.ContextHash == Guid.Empty.ToString("N")).ToList();

                // assemble entity and synonyms
                var usedEntities = new List<String>();
                common_examples.ForEach(x =>
                {
                    if (x.Entities != null)
                    {
                        usedEntities.AddRange(x.Entities.Select(y => y.Entity));
                    }
                });
                usedEntities = usedEntities.Distinct().ToList();

                var entity_synonyms = corpus.Entities.Where(x => usedEntities.Contains(x.EntityType)).ToList();

                var data = new RasaTrainingData
                {
                    Entities = entity_synonyms.Select(x => x.ToObject<RasaTraningEntity>()).ToList(),
                    UserSays = common_examples.Select(x => x.ToObject<RasaIntentExpression>()).ToList()
                };

                // meet minimal requirement
                // at least 2 different classes
                int count = data.UserSays
                    .Select(x => x.Intent)
                    .Distinct().Count();

                if (count < 2)
                {
                    data.UserSays.Add(new RasaIntentExpression
                    {
                        Intent = "Intent2",
                        Text = Guid.NewGuid().ToString("N")
                    });

                    data.UserSays.Add(new RasaIntentExpression
                    {
                        Intent = "Intent2",
                        Text = Guid.NewGuid().ToString("N")
                    });
                }

                // at least 2 corpus per intent
                data.UserSays.Select(x => x.Intent)
                .Distinct()
                .ToList()
                .ForEach(intent =>
                {
                    if(data.UserSays.Count(x => x.Intent == intent) < 2)
                    {
                        data.UserSays.Add(new RasaIntentExpression
                        {
                            Intent = intent,
                            Text = Guid.NewGuid().ToString("N")
                        });
                    }
                });

                // set empty synonym to null
                data.Entities
                    .Where(x => x.Synonyms != null)
                    .ToList()
                    .ForEach(entity =>
                    {
                        if (entity.Synonyms.Count == 0)
                        {
                            entity.Synonyms = null;
                        }
                    });

                string json = JsonConvert.SerializeObject(new { rasa_nlu_data = data },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore,
                    });

                var rest = new RestRequest("train", Method.POST);
                rest.AddQueryParameter("project", agent.Id);
                rest.AddQueryParameter("model", ctx);
                string trainingConfig = agent.Language == "zh" ? "config_jieba_mitie_sklearn.yml" : "config_mitie_sklearn.yml";
                var contentRootPatch = AppDomain.CurrentDomain.GetData("ContentRootPath").ToString();
                string body = File.ReadAllText(Path.Join(contentRootPatch, "Settings", trainingConfig));
                body = $"{body}\r\ndata: {json}";
                rest.AddParameter("application/x-yml", body, ParameterType.RequestBody);

                var response = client.Execute(rest);

                if (response.IsSuccessful)
                {
                    var result = JObject.Parse(response.Content);

                    string modelName = result["info"].Value<String>().Split(": ")[1];
                }
                else
                {
                    var result = JObject.Parse(response.Content);
                    Console.WriteLine(result["error"]);
                    result["error"].Log();
                }
            });

        }
    }
}
