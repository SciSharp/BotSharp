using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.MachineLearning.NLP;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.NERs
{
    public class WitAiEntityRecognizer : INlpPredict, INlpNer
    {
        public List<OntologyEnum> Ontologies
        {
            get
            {
                return new List<OntologyEnum>
                {
                    OntologyEnum.DateTime,
                    OntologyEnum.Location
                };
            }
        }

        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var client = new RestClient($"{Configuration.GetSection("WitAiEntityRecognizer:url").Value}");
            var request = new RestRequest(Configuration.GetSection("WitAiEntityRecognizer:resource").Value, Method.GET);
            request.AddHeader("Authorization", "Bearer " + Configuration.GetSection("WitAiEntityRecognizer:serverAccessToken").Value);
            request.AddQueryParameter("v", Configuration.GetSection("WitAiEntityRecognizer:version").Value);
            request.AddQueryParameter("q", doc.Sentences[0].Text);
            request.AddQueryParameter("verbose", "true");
            request.AddQueryParameter("autosuggest", "true");

            var result = client.Execute<WitAiResponse>(request);

            var entities = result.Data.Entities[0];
            if(entities.Datetime != null)
            {
                doc.Sentences[0].Entities.AddRange(entities.Datetime.Select(x => Map(x)));
            }

            if(entities.Location != null)
            {
                doc.Sentences[0].Entities.AddRange(entities.Location.Select(x => Map(x)));
            }

            return true;
        }

        private NlpEntity Map(WitAiEntity entity)
        {
            return new NlpEntity
            {
                Confidence = entity.Confidence,
                Start = entity.Start,
                Value = entity.Value,
                Entity = entity.Entity
            };
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            return true;
        }

        private class WitAiResponse
        {
            public List<WitAiEntityResponse> Entities { get; set; }
        }

        private class WitAiEntityResponse
        {
            public List<WitAiEntity> Location { get; set; }
            public List<WitAiEntity> Datetime { get; set; }
        }

        private class WitAiEntity
        {
            [JsonProperty("_entity")]
            public string Entity { get; set; }
            [JsonProperty("_start")]
            public int Start { get; set; }
            public string Value { get; set; }
            public decimal Confidence { get; set; }
        }
    }
}
