/*
 * BotSharp.NLP Library
 * Copyright (C) 2018 Bo Peng
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bigtree.Algorithm.Features;
using Bigtree.Algorithm.SVM;
using BotSharp.NLP.Featuring;
using BotSharp.NLP.Txt2Vec;
using Newtonsoft.Json;
using Txt2Vec;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// This is a simple (naive) classification method based on Support Vector Machine (SVM)
    /// </summary>
    public class SVMClassifier : IClassifier
    {
        private List<string> features;
        private List<Tuple<string, int>> dictionary;
        private List<string> categories;
        private RangeTransform transform;
        private Bigtree.Algorithm.SVM.Model model;
        private List<string> featuresInTfIdf;

        public void Train(List<Sentence> sentences, ClassifyOptions options)
        {
            SVMClassifierTrain(sentences, options);
        }

        public void SVMClassifierTrain(List<Sentence> sentences, ClassifyOptions options, SvmType svm = SvmType.C_SVC, KernelType kernel = KernelType.RBF, bool probability = true, string outputFile = null)
        {
            var tfidf = new TfIdfFeatureExtractor();
            tfidf.Dimension = options.Dimension;
            tfidf.Sentences = sentences;
            tfidf.CalBasedOnCategory();
            featuresInTfIdf = tfidf.Keywords();

            // copy test multiclass Model
            Problem train = new Problem();
            train.X = GetData(sentences).ToArray();
            train.Y = GetLabels(sentences).ToArray();
            train.Count = train.X.Count();
            train.MaxIndex = train.X[0].Count();//int.MaxValue;

            Parameter param = new Parameter();
            transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.Gamma = 1.0 / 3;
            param.SvmType = svm;
            param.KernelType = kernel;
            param.Probability = probability;

            int numberOfClasses = train.Y.OrderBy(x => x).Distinct().Count();
            if (numberOfClasses == 1)
            {
                Console.Write("Number of classes must greater than one!");
            }

            if (svm == SvmType.C_SVC)
            {
                for (int i = 0; i < numberOfClasses; i++)
                    param.Weights[i] = 1;
            }

            model = Training.Train(scaled, param);

            Console.Write("Training finished!");
        }

        public List<Tuple<string, double>> Classify(Sentence sentence, ClassifyOptions options)
        {
            var categoryList = new List<Tuple<string, double>>();

            var result = Predict(sentence, options).FirstOrDefault();

            for(int i = 0; i < result.Length; i++)
            {
                categoryList.Add(new Tuple<string, double>(categories[i], result[i]));
            }

            return categoryList;
        }

        public double[][] Predict(Sentence sentence, ClassifyOptions options)
        {
            Problem predict = new Problem();
            predict.X = GetData(new List<Sentence> { sentence }).ToArray();
            predict.Y = new double[1];
            predict.Count = predict.X.Count();
            predict.MaxIndex = features.Count;

            transform = options.Transform;
            Problem scaled = transform.Scale(predict);

            return Prediction.PredictLabelsProbability(model, scaled);
        }

        public List<double> GetLabels(List<Sentence> sentences)
        {
            categories = sentences.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
            List<double> labels = new List<double>();

            foreach (var sentence in sentences)
            {
                var labelId = categories.IndexOf(sentence.Label).ToString();
                labels.Add(double.Parse(labelId));
            }

            return labels;
        }

        public List<Node[]> GetData(List<Sentence> sentences)
        {
            //var extractor = new CountFeatureExtractor();
            var extractor = new Word2VecFeatureExtractor();
            extractor.Sentences = sentences;
            if(features != null)
            {
                extractor.Features = features;
            }

            if(dictionary != null)
            {
                extractor.Dictionary = dictionary;
            }

            extractor.Vectorize(featuresInTfIdf);

            if(features == null)
            {
                features = extractor.Features;
            }
            
            if(dictionary == null)
            {
                dictionary = extractor.Dictionary;
            }

            List<Node[]> datas = new List<Node[]>();

            foreach (var sentence in sentences)
            {
                List<Node> curNodes = new List<Node>();

                for(int i = 0; i < extractor.Features.Count; i++)
                {

                    int name = i;
                    /*var xx = sentence.Words.Find(x => x.Lemma == extractor.Features[i]);

                    if (xx == null)
                    {
                        curNodes.Add(new Node(name, 0));
                    }
                    else
                    {
                        curNodes.Add(new Node(name, xx.Vector));
                    }*/

                   curNodes.Add(new Node(i, sentence.Vector[i]));
                }

                datas.Add(curNodes.ToArray());
            }
            return datas;
        }

        public string SaveModel(ClassifyOptions options)
        {
            options.TransformFilePath = Path.Combine(options.ModelDir, "transform");
            options.FeaturesFileName = Path.Combine(options.ModelDir, "features");
            options.DictionaryFileName = Path.Combine(options.ModelDir, "dictionary");
            options.CategoriesFileName = Path.Combine(options.ModelDir, "categories");
            options.FeaturesInTfIdfFileName = Path.Combine(options.ModelDir, "featuresInTfIdf");

            File.WriteAllText(options.FeaturesFileName, JsonConvert.SerializeObject(features));

            File.WriteAllText(options.FeaturesInTfIdfFileName, JsonConvert.SerializeObject(featuresInTfIdf));

            File.WriteAllText(options.DictionaryFileName, JsonConvert.SerializeObject(dictionary));

            File.WriteAllText(options.CategoriesFileName, JsonConvert.SerializeObject(categories));

            RangeTransform.Write(options.TransformFilePath, transform);
            Bigtree.Algorithm.SVM.Model.Write(options.ModelFilePath, model);

            return options.ModelFilePath;
        }

        object IClassifier.LoadModel(ClassifyOptions options)
        {
            options.FeaturesFileName = Path.Combine(options.ModelDir, "features");
            options.DictionaryFileName = Path.Combine(options.ModelDir, "dictionary");
            options.ModelFilePath = Path.Combine(options.ModelDir, options.ModelName);
            options.TransformFilePath = Path.Combine(options.ModelDir, "transform");
            options.CategoriesFileName = Path.Combine(options.ModelDir, "categories");
            options.FeaturesInTfIdfFileName = Path.Combine(options.ModelDir, "featuresInTfIdf");

            features = JsonConvert.DeserializeObject<List<String>>(File.ReadAllText(options.FeaturesFileName));

            featuresInTfIdf = JsonConvert.DeserializeObject<List<String>>(File.ReadAllText(options.FeaturesInTfIdfFileName));

            dictionary = JsonConvert.DeserializeObject<List<Tuple<string, int>>>(File.ReadAllText(options.DictionaryFileName));

            categories = JsonConvert.DeserializeObject<List<String>>(File.ReadAllText(options.CategoriesFileName));
            
            model = Bigtree.Algorithm.SVM.Model.Read(options.ModelFilePath);

            options.Transform = RangeTransform.Read(options.TransformFilePath);

            return model;
        }
    }
}
