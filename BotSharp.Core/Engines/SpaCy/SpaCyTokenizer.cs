using BotSharp.Core.Abstractions;
using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTokenizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(string text, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenize", Method.GET);
            request.AddParameter("text", text);
            var response = client.Execute<Result>(request);

            data.Add("tokens", JToken.FromObject(response.Data.Tokens));

            return response.IsSuccessful;
        }

        public class Result
        {
            public List<NlpToken> Tokens { get; set; }
        }
    }
}
