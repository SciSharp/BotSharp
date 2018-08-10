using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyEntityRecognizer : INlpPipeline
    {
        List<String> entitiesInTrainingSet = new List<string>();
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            String modelPath = "./entity_rec_output";
            String newModelName = "test";
            String outputDir = "./entity_rec_output2";
            int iterTimes = 20;

            List<TrainingNode> trainingData = new List<TrainingNode>();

            var dc = new DefaultDataContextLoader().GetDefaultDc();
            /*var corpus = agent.GrabCorpus(dc);

            corpus.UserSays.ForEach(userSay =>
            {
                if (userSay.Entities != null) {
                    //texts.Add(userSay.Text);
                    List<EntityLabel> entityLabel = new List<EntityLabel>();
                    userSay.Entities.ForEach(entity => {
                        entityLabel.Add(new EntityLabel(entity.Start, entity.End, entity.Entity));
                        entitiesInTrainingSet.Add(entity.Entity);
                    });
                    trainingData.Add(new TrainingNode(userSay.Text, entityLabel));
                }
            });*/
            entitiesInTrainingSet = entitiesInTrainingSet.Distinct().ToList();
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("entityrecognizer", Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("application/json", JsonConvert.SerializeObject(new NERTrainingModel( modelPath, newModelName, outputDir, iterTimes, trainingData, entitiesInTrainingSet)), ParameterType.RequestBody);

            var response = client.Execute<Result>(request);

            return true;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            return true;
        }
    }

    public class Result
    {
        public Boolean EntityModelTrained { get; set; }
    }

    public class EntityLabel
    {
        public EntityLabel(int start, int end, string entity)
        {
            this.Start = start;
            this.End = end;
            this.Name = entity;
        }

        public int Start { get; set; }
        public int End { get; set; }
        public String Name { get; set; }

    }

    public class TrainingNode
    {
        public TrainingNode(string text, List<EntityLabel> entityLabel)
        {
            this.Text = text;
            this.Labels = entityLabel;
        }

        public String Text { get; set; }
        public List<EntityLabel> Labels { get; set; }

    }

    public class NERTrainingModel
    {
        public NERTrainingModel(string modelPath, string newModelName, string outputDir, int iterTimes, List<TrainingNode> trainingData, List<string> entitiesInTrainingSet)
        {
            this.ModelPath = modelPath;
            this.NewModelName = newModelName;
            this.OutputDir = outputDir;
            this.IterTimes = iterTimes;
            this.TrainingData = trainingData;
            this.EntitiesInTrainingSet = entitiesInTrainingSet;
        }

        public string ModelPath { set; get; }

        public string NewModelName { set; get; }

        public string OutputDir { set; get; }

        public int IterTimes { set; get; }

        public List<TrainingNode> TrainingData { set; get; }

        public List<string> EntitiesInTrainingSet { set;get;}
    }
}
