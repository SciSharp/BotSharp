using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyProvider : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool ProcessAsync(Agent agent, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("load", Method.GET);
            var response = client.Execute(request);

            return response.IsSuccessful;
        }
    }
}
