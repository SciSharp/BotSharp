using Bot.Rasa.Agents;
using Bot.Rasa.Models;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bot.Rasa.Consoles
{
    public static class RequestExtension
    {
        public static AgentResponse TextRequest(this RasaConsole console, String agentId, String text)
        {
            var client = new RestClient($"{RasaConsole.Options.HostUrl}");

            var request = new RestRequest("parse", Method.POST);
            string json = JsonConvert.SerializeObject(new { Project = agentId, Q = text },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute<AgentResponse>(request);

            return response.Data;
        }

        /// <summary>
        /// Need two categories at least
        /// </summary>
        /// <param name="console"></param>
        /// <param name="dc"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public static bool Train(this RasaConsole console, Database dc, String agentId)
        {
            var agent = dc.Table<Agent>().Find(agentId);
            var corpus = agent.GrabCorpus(dc);

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var client = new RestClient($"{RasaConsole.Options.HostUrl}");
            var request = new RestRequest("train", Method.POST);
            request.AddQueryParameter("project", agentId);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(request);

            return response.IsSuccessful;
        }
    }
}
