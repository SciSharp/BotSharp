using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
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
            List<List<decimal>> vectors = new List<List<decimal>>();
            Boolean res = true;
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.GrabCorpus(dc);

            corpus.UserSays.ForEach(usersay => {
                request.AddParameter("text", usersay.Text);
                var response = client.Execute<Result>(request);
                vectors.Add(response.Data.Vectors);
                res = res && response.IsSuccessful;
            });

            data.Add("Features", JToken.FromObject(vectors));

            return res;
        }

        public class Result
        {
            public List<decimal> Vectors { get; set; }
        }
    }
}
