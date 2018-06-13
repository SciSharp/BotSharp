using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpacyFeaturizer : INlpFeaturizer
    {
        public IConfiguration Configuration { get; set; }

        public List<decimal> Featurize(string text)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("featurize", Method.GET);
            request.AddParameter("text", text);
            var response = client.Execute<Result>(request);

            return response.Data.Vectors;
        }

        public class Result
        {
            public List<decimal> Vectors { get; set; }
        }
    }
}
