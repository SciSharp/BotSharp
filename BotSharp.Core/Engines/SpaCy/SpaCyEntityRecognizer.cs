using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyEntityRecognizer : INlpPipeline
    {
        List<String> entitiesInTrainingSet = new List<string>();
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            String modelPath = "./entity_rec_output";
            String newModelName = "test";
            String outputDir = "./entity_rec_output2";
            int iterTimes = 20;

            agent.Entities.ForEach(entity => entitiesInTrainingSet.Add(entity.Name));
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("entityrecognizer", Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("application/json", JsonConvert.SerializeObject(new { ModelPath = modelPath, NewModelName = newModelName, OutputDir = outputDir, IterTimes = iterTimes, EntitiesInTrainingSet = entitiesInTrainingSet }), ParameterType.RequestBody);

            var response = client.Execute<Result>(request);

            data["EntityModelTrained"] = response.Data.EntityModelTrained;

            return true;
        }
    }

    public class Result
    {
        public Boolean EntityModelTrained { get; set; }
    }
}
