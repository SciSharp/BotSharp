using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.MachineLearning.NLP;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BotSharp.Core.Engines.CRFsuite
{
    public class CRFsuiteEntityRecognizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;
            
            List<List<String>> tags = data["Tags"].ToObject<List<List<String>>>();
            List<List<NlpToken>> tokens = data["Tokens"].ToObject<List<List<NlpToken>>>();
            List<TrainingIntentExpression<TrainingIntentExpressionPart>> userSays = corpus.UserSays;
            List<List<TrainingData>> list = new List<List<TrainingData>>();

            var dir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "TrainingFiles");
            FileStream fs = new FileStream(Path.Join(dir, "rawTrain.txt"), FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0 ; i < tags.Count; i++) 
            {
                List<TrainingData> curLine = Merge(tokens[i], tags[i], userSays[i].Entities);
                list.Add(curLine);
                curLine.ForEach(trainingData =>{
                    string[] wordParams = {trainingData.Entity, trainingData.Token, trainingData.Tag, trainingData.Chunk};
                    string wordStr = string.Join(" ", wordParams);
                    sw.Write(wordStr + "\n");
                });
                sw.Write("\n");
            }      
            sw.Flush();
            sw.Close();
            fs.Close();  
            
            new MachineLearning.CRFsuite.Ner().NerStart();
            
            Runcmd();

            return true;
        }

        public void Runcmd () 
        {
            var algorithmDir = Path.Join(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms");
            var dataDir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "TrainingFiles");

            string cmd = $"{algorithmDir}/crfsuite learn -m {dataDir}/crfsuite/bolo.model {dataDir}/crfsuite/1.txt";
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "sh";
            p.StartInfo.UseShellExecute = false; 
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;
            p.Start();

            p.StandardInput.WriteLine(cmd + "&exit");
            p.StandardInput.AutoFlush = false;

            string output = p.StandardOutput.ReadToEnd();

            p.WaitForExit();//等待程序执行完退出进程
            p.Close();
            Console.WriteLine(output);
        }
        public List<TrainingData> Merge(List<NlpToken> sentence, List<string> tags, List<TrainingIntentExpressionPart> entities)
        {
            List<TrainingData> trainingTuple = new List<TrainingData>();
            HashSet<String> entityWordBag = new HashSet<String>();
            int wordCandidateCount = 0;
            
            for (int i = 0; i < sentence.Count; i++)
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
                                if (sentence[i + j].Text == words[j])
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
                                    trainingTuple.Add(new TrainingData(entityName, s, tags[i], "I"));
                                }
                                entityFinded = true;
                            }
                        }
                    });
                }
                if (wordCandidateCount == 0)
                {
                    trainingTuple.Add(new TrainingData("O", sentence[i].Text, tags[i], "O"));
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
        public String Tag { get; set; }
        public String Chunk { get; set; }

        public TrainingData(string entity, string token, string tag, string chunk)
        {
            this.Token = token;
            this.Entity = entity;
            this.Tag = tag;
            this.Chunk = chunk;
        }
    }


    public class Token
    {
        public String Text { get; set; }
        public int Offset { get; set; }
        public int End { get; set; }
    }

    public class Entity
    {
        public String EntityName { get; set; }
        public String Value { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }


}
