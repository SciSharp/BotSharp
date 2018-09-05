using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP.Classify;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
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
            /*
            //
            var client = new RestClient("http://10.2.21.200:5005");
            var request = new RestRequest("doc2vec", Method.GET);
            request.AddParameter("text", doc.Sentences[0].Text);
            var response = client.Execute<PredResult>(request);
            PredResult pred = JsonConvert.DeserializeObject<PredResult>(response.Content);

            Vec vec = new Vec();
            vec.VecNodes = pred.Doc2Vec;

            LabeledFeatureSet featureSet = svmClassifier.FeatureSetsGenerator(vec, "");
            //
            */
            ClassifyOptions classifyOptions = new ClassifyOptions();
            classifyOptions.Model = SVM.BotSharp.MachineLearning.Model.Read(Path.Combine(Settings.ModelDir, "svm_classifier_model"));
            classifyOptions.Transform = SVM.BotSharp.MachineLearning.RangeTransform.Read(Path.Combine(Settings.ModelDir, "transform_obj_data"));
            double[][] d = svmClassifier.Predict(featureSet, classifyOptions);

            string intent = null;
            decimal confidence = 0;
            double max = Double.MinValue;
            for (int i = 0; i < d[0].Count(); i++)
            {
                if (d[0][i] > max)
                {
                    max = d[0][i];
                    intent = agent.Intents[i].Name;
                    confidence = (decimal)d[0][i];
                }
            }

            File.Delete(predictFileName);

            doc.Sentences[0].Intent = new TextClassificationResult
            {
                Classifier = "SVMClassifier",
                Label = intent,
                Confidence = confidence
            };

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

            /*
            // try using spacy doc2vec
            var client = new RestClient("http://10.2.21.200:5005");
            var request = new RestRequest("batchdoc2vec", Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("application/json", JsonConvert.SerializeObject(new {Sentences = sentences}), ParameterType.RequestBody);

            var response = client.Execute<Result>(request);
            Result res = JsonConvert.DeserializeObject<Result>(response.Content);

            List<Vec> vecs = new List<Vec>();
            foreach (List<double> cur in res.Doc2vecList)
            {
                Vec vec = new Vec();
                vec.VecNodes = cur;
                vecs.Add(vec);
            }
            List<LabeledFeatureSet> featureSetList = svmClassifier.FeatureSetsGenerator(vecs, labels);
            //
            */



            ClassifyOptions classifyOptions = new ClassifyOptions();
            classifyOptions.ModelFilePath = Path.Combine(Settings.ModelDir, "svm_classifier_model");
            classifyOptions.TransformFilePath = Path.Combine(Settings.ModelDir, "transform_obj_data");
            svmClassifier.Train(featureSetList, classifyOptions);

            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 31, 2018";
            return true;
        }
    }
    public class Result
    {
        public List<List<double>> Doc2vecList { get; set; }
    }

    public class PredResult
    {
        public List<double> Doc2Vec{ get; set; }
    }
}
