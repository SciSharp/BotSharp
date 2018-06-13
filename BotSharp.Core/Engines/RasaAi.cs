using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Models;
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

            // remove Default Fallback Intent
            corpus.UserSays = corpus.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

#if RASA_NLU_0_11
            rest.AddParameter("application/json", json, ParameterType.RequestBody);
#else
            string trainingConfig = agent.Language == "zh" ? "config_jieba_mitie_sklearn.yml" : "config_mitie_sklearn.yml";
            string body = File.ReadAllText($"{Database.ContentRootPath}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}{trainingConfig}");
            body = $"{body}\r\ndata: {json}";
            rest.AddParameter("application/x-yml", body, ParameterType.RequestBody);
#endif

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
    }
}
