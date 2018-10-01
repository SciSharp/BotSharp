using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using Bigtree.Algorithm.CRFLite;
using Bigtree.Algorithm.CRFLite.Decoder;
using Bigtree.Algorithm.CRFLite.Encoder;
using BotSharp.Models.NLP;
using BotSharp.NLP.Tokenize;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Platform.Models;

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
                            string[] wordParams = { trainingData.Token, trainingData.Pos, trainingData.Entity };
                            string wordStr = string.Join("\t", wordParams);
                            sw.WriteLine(wordStr);
                        });
                        list.Add(curLine);
                        sw.WriteLine();
                    }
                    sw.Flush();
                }
            }

            string contentDir = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            string template = Configuration.GetValue<String>($"template");
            template = template.Replace("|App_Data|", contentDir + System.IO.Path.DirectorySeparatorChar);

            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
                TrainingCorpusFileName = rawTrainingDataFileName,
                TemplateFileName = template,
                ModelFileName = modelFileName,
            });

            return result;
        }

        private List<TrainingData> Merge(NlpDoc doc, List<Token> tokens, List<TrainingIntentExpressionPart> entities)
        {
            List<TrainingData> trainingTuple = new List<TrainingData>();
            HashSet<String> entityWordBag = new HashSet<String>();
            int wordCandidateCount = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                TrainingIntentExpressionPart curEntity = null;
                if (entities == null) continue;

                bool entityFinded = false;
                for (int entityIndex = 0; entityIndex < entities.Count; entityIndex++)
                {
                    var entity = entities[entityIndex];

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

                            for(int wordIndex = 0; wordIndex < words.Length; wordIndex++)
                            {
                                var tag = entityName;

                                if (wordIndex == 0)
                                {
                                    if (words.Length == 1)
                                    {
                                        tag = "S_" + entityName;
                                    }
                                    else
                                    {
                                        tag = "B_" + entityName;
                                    }
                                }
                                else if (wordIndex == words.Length - 1)
                                {
                                    tag = "E_" + entityName;
                                }
                                else
                                {
                                    tag = "M_" + entityName;
                                }

                                var word = words[wordIndex];
                                trainingTuple.Add(new TrainingData(tag, word, tokens[i].Pos));
                            }

                            entityFinded = true;
                        }
                    }
                }

                if (wordCandidateCount == 0)
                {
                    trainingTuple.Add(new TrainingData("S", tokens[i].Text, tokens[i].Pos));
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
            var decoder = new CRFDecoder();
            var options = new DecoderOptions
            {
                ModelFileName = System.IO.Path.Combine(Settings.ModelDir, meta.Model)
            };

            //Load encoded model from file
            decoder.LoadModel(options.ModelFileName);

            //Create decoder tagger instance.
            var tagger = decoder.CreateTagger(options.NBest, options.MaxWord);
            tagger.set_vlevel(options.ProbLevel);

            //Initialize result
            var crf_out = new CRFSegOut[options.NBest];
            for (var i = 0; i < options.NBest; i++)
            {
                crf_out[i] = new CRFSegOut(options.MaxWord);
            }

            doc.Sentences.ForEach(sent =>
            {
                List<List<String>> dataset = new List<List<string>>();
                dataset.AddRange(sent.Tokens.Select(token => new List<String> { token.Text, token.Pos }).ToList());
                //predict given string's tags
                decoder.Segment(crf_out, tagger, dataset);

                var entities = new List<NlpEntity>();

                for (int i = 0; i < sent.Tokens.Count; i++)
                {
                    var entity = crf_out[0].result_;
                    entities.Add(new NlpEntity
                    {
                        Entity = entity[i],
                        Start = doc.Sentences[0].Tokens[i].Start,
                        Value = doc.Sentences[0].Tokens[i].Text,
                        Confidence = 0,
                        Extrator = "BotSharpCRFNer"
                    });
                }

                sent.Entities = MergeEntity(doc.Sentences[0].Text, entities);
            });

            return true;
        }

        private List<NlpEntity> MergeEntity(string sentence, List<NlpEntity> tokens)
        {
            List<NlpEntity> res = new List<NlpEntity>();

            for(int i = 0; i < tokens.Count; i++)
            {
                var entity = tokens[i];

                if (entity.Entity.StartsWith("S_"))
                {
                    entity.Entity = entity.Entity.Split('_')[1];
                    res.Add(entity);
                }
                else if (entity.Entity.StartsWith("B_"))
                {
                    entity.Entity = entity.Entity.Split('_')[1];

                    for(int j = i; j < tokens.Count; j++)
                    {
                        var token = tokens[j];
                        if (token.Entity.StartsWith("E_"))
                        {
                            res.Add(new NlpEntity
                            {
                                Value = sentence.Substring(entity.Start, token.End - entity.Start + 1),
                                Entity = entity.Entity,
                                Extrator = entity.Extrator,
                                Start = entity.Start,
                                Confidence = entity.Confidence
                            });
                        }

                        i++;
                    }
                }
            }

            return res;
        }

        public class TrainingData
        {
            public String Token { get; set; }
            public String Entity { get; set; }
            public String Pos { get; set; }

            public TrainingData(string entity, string token, string pos)
            {
                Token = token;
                Entity = entity;
                Pos = pos;
            }
        }
    }
}
