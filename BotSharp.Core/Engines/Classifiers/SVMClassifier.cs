using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP.Classify;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Txt2Vec;

namespace BotSharp.Core.Engines.Classifiers
{
    public class SVMClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);
            string predictFileName = Path.Combine(Settings.TempDir, "fasttext.txt");
            File.WriteAllText(predictFileName, doc.Sentences[0].Text);

            var output = CmdHelper.Run(Path.Combine(Settings.AlgorithmDir, "fasttext"), $"predict-prob \"{modelFileName}.bin\" \"{predictFileName}\"");

            File.Delete(predictFileName);

            doc.Sentences[0].Intent = new TextClassificationResult
            {
                Classifier = "FasttextClassifier",
                Label = output.Split(' ')[0].Split(new string[] { "__label__" }, StringSplitOptions.None)[1],
                Confidence = decimal.Parse(output.Split(' ')[1])
            };

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            meta.Model = "classification-fasttext.model";

            string parsedTrainingDataFileName = Path.Combine(Settings.TempDir, $"classification-fasttext.parsed.txt");
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            // assemble corpus
            StringBuilder corpus = new StringBuilder();
            agent.Corpus.UserSays.ForEach(x => corpus.AppendLine($"__label__{x.Intent} {x.Text}"));

            List<string> labels = new List<string>();
            List<string> sentences = new List<string>();
            

            agent.Corpus.UserSays.ForEach(x =>{
                labels.Add(x.Intent);
                sentences.Add(x.Text);
            });

            Dictionary<string, string>  labelDic = new Dictionary<string, string>();
            int num = 0;
            foreach (string label in labels)
            {
                if (labelDic.ContainsKey(label))
                {
                    continue;
                }
                labelDic.Add(label, num++.ToString());
            };
            List<string> labelNums = new List<string>();
            foreach (string label in labels)
            {
                labelNums.Add(labelDic[label]);
            }
            NLP.Classify.SVMClassifier svmClassifier = new NLP.Classify.SVMClassifier();
            Args args = new Args(); 
            //args.WordDecoderModelFile = Path.Combine(Settings.ModelDir, "wordvec_enu.bin");
            //List<LabeledFeatureSet> featureSetList = svmClassifier.FeatureSetsGenerator(new VectorGenerator(args).Sentence2Vec(sentences), labelNums);
            //svmClassifier.Train(featureSetList, new ClassifyOptions(Path.Combine(Settings.ModelDir, "svm_classifier_model")));

            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 31, 2018";


            return true;
        }
    }
}
