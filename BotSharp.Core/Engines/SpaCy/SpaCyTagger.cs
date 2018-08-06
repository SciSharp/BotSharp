using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.MachineLearning.NLP;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTagger : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        
        public bool Process(Agent agent, JObject data)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tagger", Method.GET);
            List<List<String>> tags = new List<List<String>>();
            Boolean res = true;
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;

            corpus.UserSays.ForEach(usersay => {
                request.AddParameter("text", usersay.Text);
                var response = client.Execute<Result>(request);
                tags.Add(response.Data.Tags);
                res = res && response.IsSuccessful;
            });
            data.Add("Tags", JToken.FromObject(tags));

            return res;
        }

        public class Result
        {
            public List<String> Tags { get; set; }
        }
    }
}
