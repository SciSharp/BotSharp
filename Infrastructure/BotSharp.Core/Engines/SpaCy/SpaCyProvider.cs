using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyProvider : INlpProvider
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Load(Agent agent, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("load", Method.GET);
            var response = client.Execute<Result>(request);

            meta.Meta = JObject.FromObject(response.Data);
            meta.Meta.Remove("models");
            meta.Model = response.Data.Models;

            return response.IsSuccessful;
        }

        private class Result
        {
            [JsonProperty("spaCy ver")]
            public string Version { get; set; }
            [JsonProperty("models")]
            public string Models { get; set; }
            [JsonProperty("python ver")]
            public string Python { get; set; }
        }
    }
}
