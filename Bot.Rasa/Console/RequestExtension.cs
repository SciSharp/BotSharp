using Bot.Rasa.Agents;
using Bot.Rasa.Models;
using CustomEntityFoundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bot.Rasa.Console
{
    public static class RequestExtension
    {
        public static AgentResponse TextRequest(this RasaConsole console, String agentId, String text)
        {
            var client = new RestClient($"{console.options.HostUrl}");

            var request = new RestRequest("parse?project={project}&q={text}", Method.GET);
            request.AddUrlSegment("project", agentId);
            request.AddUrlSegment("text", text);

            var response = client.Execute<AgentResponse>(request);

            return response.Data;
        }

        public static bool Train(this RasaConsole console, EntityDbContext dc, String agentId)
        {
            var agent = dc.Agent().Find(agentId);
            var corpus = agent.GrabCorpus(dc);

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var client = new RestClient($"{console.options.HostUrl}");
            var request = new RestRequest("train", Method.POST);
            request.AddQueryParameter("project", agentId);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(request);

            return true;
        }
    }
}
