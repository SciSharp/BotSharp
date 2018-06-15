using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpacyFeaturizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("featurize", Method.GET);
            request.AddParameter("text", "");
            var response = client.Execute<Result>(request);

            data.Add("Features", JToken.FromObject(response.Data.Vectors));

            return response.IsSuccessful;
        }

        public class Result
        {
            public List<decimal> Vectors { get; set; }
        }
    }
}
