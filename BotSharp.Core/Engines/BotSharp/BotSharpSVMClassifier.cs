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

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpSVMClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);
            string predictFileName = Path.Combine(Settings.TempDir, "svm-predict-tempfile.txt");
            File.WriteAllText(predictFileName, doc.Sentences[0].Text);

            NLP.Classify.SVMClassifier svmClassifier = new NLP.Classify.SVMClassifier();
            Args args = new Args();
            args.ModelFile = Path.Combine(Configuration.GetValue<String>("BotSharpSVMClassifier:wordvec"), "wordvec_enu.bin");
            LabeledFeatureSet featureSet = svmClassifier.FeatureSetsGenerator(new VectorGenerator(args).SingleSentence2Vec(doc.Sentences[0].Text), "");

            ClassifyOptions classifyOptions = new ClassifyOptions();
            classifyOptions.Model = SVM.BotSharp.MachineLearning.Model.Read(Path.Combine(Settings.ModelDir, "svm_classifier_model")); 
            double[][] d = svmClassifier.Predict(featureSet, classifyOptions);

            File.Delete(predictFileName);

            //doc.Sentences[0].Intent = new TextClassificationResult
            //{
            //    Classifier = "FasttextClassifier",
            //    Label = output.Split(' ')[0].Split(new string[] { "__label__" }, StringSplitOptions.None)[1],
            //    Confidence = decimal.Parse(output.Split(' ')[1])
            //};

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            meta.Model = "classification-svm.model";
            string parsedTrainingDataFileName = Path.Combine(Settings.TempDir, $"classification-svm.parsed.txt");
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            List<string> labels = new List<string>();
            List<string> sentences = new List<string>();

            agent.Corpus.UserSays.ForEach(x =>{
                agent.Intents.ForEach(intent => {
                    if (intent.Name == x.Intent)
                    {
                        labels.Add(agent.Intents.IndexOf(intent).ToString());
                    }
                });
                sentences.Add(x.Text);
            });

            NLP.Classify.SVMClassifier svmClassifier = new NLP.Classify.SVMClassifier();
            Args args = new Args(); 
            args.ModelFile = Path.Combine(Configuration.GetValue<String>("BotSharpSVMClassifier:wordvec"), "wordvec_enu.bin");
            List<LabeledFeatureSet> featureSetList = svmClassifier.FeatureSetsGenerator(new VectorGenerator(args).Sentence2Vec(sentences), labels);
            ClassifyOptions classifyOptions = new ClassifyOptions();
            classifyOptions.ModelFilePath = Path.Combine(Settings.ModelDir, "svm_classifier_model");
            svmClassifier.Train(featureSetList, classifyOptions);

            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 31, 2018";


            return true;
        }
    }
}
