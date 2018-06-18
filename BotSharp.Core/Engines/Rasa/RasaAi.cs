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
    /// Rasa nlu 0.12.x
    /// </summary>
    public class RasaAi : IBotPlatform
    {
        public Database dc { get; set; }
        public AIConfiguration AiConfig { get; set; }

        public Agent agent { get; set; }

        public RasaAi(Database dc)
        {
            this.dc = dc;
        }

        public RasaAi(Database dc, AIConfiguration aiConfig)
        {
            this.dc = dc;

            AiConfig = aiConfig;
            agent = this.LoadAgent(dc, aiConfig);
            aiConfig.DevMode = agent.DeveloperAccessToken == aiConfig.ClientAccessToken;
        }

        public string Train()
        {
            var client = new RestClient($"{Database.Configuration.GetSection("Rasa:Nlu").Value}");
            var rest = new RestRequest("train", Method.POST);
            rest.AddQueryParameter("project", agent.Id);

            var corpus = agent.GrabCorpus(dc);

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

            string trainingConfig = agent.Language == "zh" ? "config_jieba_mitie_sklearn.yml" : "config_spacy.yml";
            string body = File.ReadAllText($"{Database.ContentRootPath}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}{trainingConfig}");
            body = $"{body}\r\ndata: {json}";
            rest.AddParameter("application/x-yml", body, ParameterType.RequestBody);

            var response = client.Execute(rest);

            if (response.IsSuccessful)
            {
                var result = JObject.Parse(response.Content);

                string modelName = result["info"].Value<String>().Split(": ")[1];

                return modelName;
            }
            else
            {
                var result = JObject.Parse(response.Content);

                Console.WriteLine(result["error"]);

                return String.Empty;
            }
        }

        public void TrainWithContexts()
        {
            var corpus = agent.GrabCorpus(dc);

            var client = new RestClient($"{Database.Configuration.GetSection("Rasa:Nlu").Value}");

            var contextHashs = corpus.UserSays
                .Select(x => x.ContextHash)
                .Distinct()
                .ToList();

            contextHashs.ForEach(ctx =>
            {
                var data = new RasaTrainingData
                {
                    Entities = corpus.Entities,
                    UserSays = corpus.UserSays.Where(x => x.ContextHash == ctx).ToList()
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

                string json = JsonConvert.SerializeObject(new { rasa_nlu_data = data },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore
                    });

                var rest = new RestRequest("train", Method.POST);
                rest.AddQueryParameter("project", agent.Id);
                rest.AddQueryParameter("model", ctx);
                string trainingConfig = agent.Language == "zh" ? "config_jieba_mitie_sklearn.yml" : "config_spacy.yml";
                string body = File.ReadAllText($"{Database.ContentRootPath}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}{trainingConfig}");
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
