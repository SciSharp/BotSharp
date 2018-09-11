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
using BotSharp.Algorithm.Features;
using SVM.BotSharp.MachineLearning;
using Txt2Vec;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// This is a simple (naive) classification method based on Support Vector Machine (SVM)
    /// </summary>
    public class SVMClassifier : IClassifier
    {
        public double[][] Predict(FeaturesWithLabel featureSet, ClassifyOptions options)
        {
            Problem predict = new Problem();
            List<FeaturesWithLabel> featureSets = new List<FeaturesWithLabel>();
            featureSets.Add(featureSet);
            predict.X = GetData(featureSets).ToArray();
            predict.Y = new double[1];
            predict.Count = predict.X.Count();
            predict.MaxIndex = 300;

            RangeTransform transform = options.Transform;
            Problem scaled = transform.Scale(predict);

            return Prediction.PredictLabelsProbability(options.Model, scaled);
        }

        public void Train(List<Tuple<string, double[]>> featureSets, double[] values, ClassifyOptions options)
        {
            // SVMClassifierTrain(featureSets, options);
        }

        public List<Tuple<string, double>> Classify(double[] features, ClassifyOptions options)
        {
            throw new NotImplementedException();
        }

        public void SVMClassifierTrain(List<FeaturesWithLabel> featureSets, ClassifyOptions options, SvmType svm = SvmType.C_SVC, KernelType kernel = KernelType.RBF, bool probability = true, string outputFile = null)
        {   
            // copy test multiclass Model
            Problem train = new Problem();
            train.X = GetData(featureSets).ToArray();
            train.Y = GetLabels(featureSets).ToArray();
            train.Count = train.X.Count();
            train.MaxIndex = 300;//int.MaxValue;

            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.Gamma = 1.0 / 3;
            param.SvmType = svm;
            param.KernelType = kernel;
            param.Probability = probability;

            int numberOfClasses = train.Y.Distinct().Count();
            if (numberOfClasses == 1)
            {
                throw new ArgumentException("Number of classes can't be one!");
            }
            if (svm == SvmType.C_SVC)
            {
                for (int i = 0; i < numberOfClasses; i++)
                    param.Weights[i] = 1;
            }
            var model = Training.Train(scaled, param);
            RangeTransform.Write(options.TransformFilePath, transform);
            SVM.BotSharp.MachineLearning.Model.Write(options.ModelFilePath, model);
            Console.Write("Training finished!");
        }

        public List<double> GetLabels(List<FeaturesWithLabel> featureSets)
        {
            List<double> labels = new List<double>();
            foreach (var labelFeatureSet in featureSets)
            {
                labels.Add(double.Parse(labelFeatureSet.Label));
            }

            return labels;
        }

        public List<Node[]> GetData(List<FeaturesWithLabel> featureSets)
        {
            List<Node[]> datas = new List<Node[]>();

            foreach (var labelFeatureSet in featureSets)
            {
                List<Node> curNodes = new List<Node>();
                labelFeatureSet.Features.ForEach(features => {
                    int name = Int32.Parse(features.Name);
                    double value = double.Parse(features.Value);
                    curNodes.Add(new Node(name, value));
                });
                datas.Add(curNodes.ToArray());
            }
            return datas;
        }

        public List<FeaturesWithLabel> FeatureSetsGenerator(List<Vec> sentenceVectors, List<String> labels)
        {
            var res = new List<FeaturesWithLabel>();
            int j;
            for (int i = 0; i < labels.Count; i++)
            {
                string curLabel = labels[i];
                Vec curVec = sentenceVectors[i];
                var labeledFeatureSet = new FeaturesWithLabel();
                j = 1;
                foreach (double node in curVec.VecNodes)
                {
                    Feature feature = new Feature((j++).ToString(), node.ToString());
                    labeledFeatureSet.Features.Add(feature);
                }
                labeledFeatureSet.Label = curLabel;
                res.Add(labeledFeatureSet);
            }

            return res;
        }

        public FeaturesWithLabel FeatureSetsGenerator(Vec sentenceVectors, String label)
        {
            var labeledFeatureSet = new FeaturesWithLabel();
            int j = 1;
            foreach (double node in sentenceVectors.VecNodes)
            {
                Feature feature = new Feature((j++).ToString(), node.ToString());
                labeledFeatureSet.Features.Add(feature);
            }
            labeledFeatureSet.Label = label;

            return labeledFeatureSet;
        }
    }
}
