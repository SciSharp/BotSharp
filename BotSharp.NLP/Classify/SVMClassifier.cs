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
using SVM.BotSharp.MachineLearning;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// This is a simple (naive) classification method based on Support Vector Machine (SVM)
    /// </summary>
    public class SVMClassifier : IClassifier
    {
        public void Classify(LabeledFeatureSet featureSet, ClassifyOptions options)
        {
            Problem test = new Problem();
            List<LabeledFeatureSet> featureSets = new List<LabeledFeatureSet>();
            featureSets.Add(featureSet);
            test.X = GetData(featureSets).ToArray();
            test.Y = GetLabels(featureSets).ToArray();
            test.Count = test.Y.Distinct().Count();
            test.MaxIndex = int.MaxValue;

            RangeTransform transform = RangeTransform.Compute(test);
            Problem scaled = transform.Scale(test);
            double d = Prediction.Predict(scaled, options.PrediceOutputFile, options.Model, false);
        }

        public void Train(List<LabeledFeatureSet> featureSets, ClassifyOptions options)
        {
            SVMClassifierTrain(featureSets, options);
        }

        public void SVMClassifierTrain (List<LabeledFeatureSet> featureSets, ClassifyOptions options, SvmType svm = SvmType.C_SVC, KernelType kernel = KernelType.RBF, bool probability = true, string outputFile = null) 
        {
            // copy test multiclass Model
            Problem train = new Problem();
            train.X = GetData(featureSets).ToArray();
            train.Y = GetLabels(featureSets).ToArray();
            train.Count = train.Y.Distinct().Count();
            train.MaxIndex = int.MaxValue;

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
            Model model = Training.Train(scaled, param);
            Model.Write(options.ModelFilePath, model);
        }

        public List<double> GetLabels (List<LabeledFeatureSet> featureSets) 
        {
            List<double> labels = new List<double>();
            foreach (LabeledFeatureSet labelFeatureSet in featureSets)
            {
                labels.Add(double.Parse(labelFeatureSet.Label));
            }

            return labels;
        }

        public List<Node[]> GetData(List<LabeledFeatureSet> featureSets)
        {
            List<Node[]> datas = new List<Node[]>();

            foreach (LabeledFeatureSet labelFeatureSet in featureSets)
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
    }

    
}
