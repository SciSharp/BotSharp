using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, JObject data, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("featurize", Method.GET);
            List<List<decimal>> vectors = new List<List<decimal>>();
            Boolean res = true;
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            /*var corpus = agent.GrabCorpus(dc);

            corpus.UserSays.ForEach(usersay => {
                request.AddParameter("text", usersay.Text);
                var response = client.Execute<Result>(request);
                vectors.Add(response.Data.Vectors);
                res = res && response.IsSuccessful;
            });*/

            data.Add("Features", JToken.FromObject(vectors));

            return res;
        }

        public async Task<bool> Predict(Agent agent, JObject data, PipeModel meta)
        {
            return true;
        }

        public class Result
        {
            public List<decimal> Vectors { get; set; }
        }
    }
}
