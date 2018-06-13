using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTokenizer : INlpTokenizer
    {
        public IConfiguration Configuration { get; set; }

        public List<NlpToken> Tokenize(string text)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenize", Method.GET);
            request.AddParameter("text", text);
            var response = client.Execute<Result>(request);

            return response.Data.Tokens;
        }

        public class Result
        {
            public List<NlpToken> Tokens { get; set; }
        }
    }
}
