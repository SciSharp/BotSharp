using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Models.CRFLite;
using BotSharp.Models.CRFLite.Encoder;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpCRFNer : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var corpus = agent.Corpus;

            meta.Model = "ner-crf.model";

            List<TrainingIntentExpression<TrainingIntentExpressionPart>> userSays = corpus.UserSays;
            List<List<TrainingData>> list = new List<List<TrainingData>>();

            string rawTrainingDataFileName = System.IO.Path.Combine(Settings.TempDir, "ner-crf.corpus.txt");
            string modelFileName = System.IO.Path.Combine(Settings.ModelDir, meta.Model);

            using (FileStream fs = new FileStream(rawTrainingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; i < doc.Sentences.Count; i++)
                    {
                        List<TrainingData> curLine = Merge(doc, doc.Sentences[i].Tokens, userSays[i].Entities);
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

            string contentDir = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            string template = Configuration.GetValue<String>($"BotSharpCRFNer:template");
            template = template.Replace("|App_Data|", contentDir);

            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
                TrainingCorpusFileName = rawTrainingDataFileName,
                TemplateFileName = template,
                ModelFileName = modelFileName,
            });

            return result;
        }

        public List<TrainingData> Merge(NlpDoc doc, List<Token> tokens, List<TrainingIntentExpressionPart> entities)
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
                            var vDoc = new NlpDoc { Sentences = new List<NlpDocSentence> { new NlpDocSentence { Text = entity.Value } } };
                            doc.Tokenizer.Predict(null, vDoc, null);
                            string[] words = vDoc.Sentences[0].Tokens.Select(x => x.Text).ToArray();

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
                            if (wordCandidateCount != 0) // && entity.Start == tokens[i].Offset)
                            {
                                String entityName = curEntity.Entity.Contains(":") ? curEntity.Entity.Substring(curEntity.Entity.IndexOf(":") + 1) : curEntity.Entity;
                                foreach (string s in words)
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

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            throw new NotImplementedException();
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
}
