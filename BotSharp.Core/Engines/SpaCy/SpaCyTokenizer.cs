using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using BotSharp.MachineLearning.NLP;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTokenizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenize", Method.GET);
            List<List<NlpToken>> tokens = new List<List<NlpToken>>();
            Boolean res = true;
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;

            corpus.UserSays.ForEach(usersay => {
                request.AddParameter("text", usersay.Text);
                var response = client.Execute<Result>(request);
                tokens.Add(response.Data.Tokens);
                res = res && response.IsSuccessful;
            });


            

            data.Add("Tokens", JToken.FromObject(tokens));

            return res;
        }
        

        public class Result
        {
            public List<NlpToken> Tokens { get; set; }
        }
    }
}
