using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyEntitizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("entitize", Method.GET);
            request.AddParameter("text", "");
            var response = client.Execute<Result>(request);

            //data.Add("Entities", JToken.FromObject(response.Data.Entities));

            return response.IsSuccessful;
        }

        public class Result
        {
            public List<NlpEntity> Entities { get; set; }
        }
    }
}
