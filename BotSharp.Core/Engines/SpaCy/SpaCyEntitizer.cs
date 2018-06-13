using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyEntitizer : INlpEntitizer
    {
        public IConfiguration Configuration { get; set; }

        public List<NlpEntity> Entitize(string text)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("entitize", Method.GET);
            request.AddParameter("text", text);
            var response = client.Execute<Result>(request);

            return response.Data.Entities;
        }

        public class Result
        {
            public List<NlpEntity> Entities { get; set; }
        }
    }
}
