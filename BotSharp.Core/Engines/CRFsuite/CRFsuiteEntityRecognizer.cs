using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.MachineLearning.NLP;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.CRFsuite
{
    public class CRFsuiteEntityRecognizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool ProcessAsync(Agent agent, JObject data)
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;

            List<List<NlpToken>> tokens = data["Tokens"].ToObject<List<List<NlpToken>>>();
            List<TrainingIntentExpression<TrainingIntentExpressionPart>> userSays = corpus.UserSays;
            List<List<TrainingData>> list = new List<List<TrainingData>>();

            var dir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "TrainingFiles");
            string rawTrainingDataFileName = Path.Join(dir, $"{agent.Id}.corpus.txt");
            string parsedTrainingDataFileName = Path.Join(dir, $"{agent.Id}.parsed.txt");
            string modelFileName = Path.Join(dir, $"{agent.Id}.model");
            string logFileName = Path.Join(dir, $"{agent.Id}.log.txt");

            using (FileStream fs = new FileStream(rawTrainingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        List<TrainingData> curLine = Merge(tokens[i], userSays[i].Entities);
                        curLine.ForEach(trainingData =>
                        {
                            string[] wordParams = { trainingData.Entity, trainingData.Token, trainingData.Pos, trainingData.Chunk };
                            string wordStr = string.Join(" ", wordParams);
                            sw.Write(wordStr + "\n");
                        });
                        list.Add(curLine);
                        sw.Write("\n");
                    }
                    sw.Flush();
                }
            }


            var uniFeatures = Configuration.GetValue<String>($"CRFsuiteEntityRecognizer:uniFeatures").Split(" ");
            var biFeatures = Configuration.GetValue<String>($"CRFsuiteEntityRecognizer:biFeatures").Split(" ");

            new MachineLearning.CRFsuite.Ner()
                .NerStart(rawTrainingDataFileName, parsedTrainingDataFileName, uniFeatures, biFeatures);

            var algorithmDir = Path.Join(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms");

            CmdHelper.Run(Path.Join(algorithmDir, "crfsuite"), $"learn -m {modelFileName} {parsedTrainingDataFileName}"); // --split=3 -x

            Console.WriteLine($"Saved model to {modelFileName}");

            return true;
        }

        public List<TrainingData> Merge(List<NlpToken> tokens, List<TrainingIntentExpressionPart> entities)
        {
            List<TrainingData> trainingTuple = new List<TrainingData>();
            HashSet<String> entityWordBag = new HashSet<String>();
            int wordCandidateCount = 0;
            
            for (int i = 0; i < tokens.Count; i++)
            {
                TrainingIntentExpressionPart curEntity = null;
                if (entities != null) 
                {
                    bool entityFinded = false;
                    entities.ForEach(entity => {
                        if (!entityFinded)
                        {
                            string[] words = entity.Value.Split(" ");
                            for (int j = 0; j < words.Length; j++)
                            {
                                if (tokens[i + j].Text == words[j])
                                {
                                    wordCandidateCount++;
                                    if (j == words.Length - 1)
                                    {
                                        curEntity = entity;
                                    }
                                }
                                else
                                {
                                    wordCandidateCount = 0;
                                    break;
                                }
                            }
                            if (wordCandidateCount != 0)
                            {
                                String entityName = curEntity.Entity.Contains(":")? curEntity.Entity.Substring(curEntity.Entity.IndexOf(":") + 1): curEntity.Entity;
                                foreach(string s in words) 
                                {
                                    trainingTuple.Add(new TrainingData(entityName, s, tokens[i].Pos, "I"));
                                }
                                entityFinded = true;
                            }
                        }
                    });
                }
                if (wordCandidateCount == 0)
                {
                    trainingTuple.Add(new TrainingData("O", tokens[i].Text, tokens[i].Pos, "O"));
                }
                else
                {
                    i = i + wordCandidateCount - 1;
                }
            }
            return trainingTuple;

        }
    }

    public class TrainingData
    {
        public String Token { get; set; }
        public String Entity { get; set; }
        public String Pos { get; set; }
        public String Chunk { get; set; }

        public TrainingData(string entity, string token, string pos, string chunk)
        {
            Token = token;
            Entity = entity;
            Pos = pos;
            Chunk = chunk;
        }
    }
}
