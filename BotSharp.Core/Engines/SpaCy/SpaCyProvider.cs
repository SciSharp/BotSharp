using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyProvider : INlpProvider
    {
        public IConfiguration Configuration { get; set; }

        public bool Load()
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("load", Method.GET);
            var response = client.Execute(request);

            return response.IsSuccessful;
        }
    }
}
