/*
 * BotSharp.NLP Library
 * Copyright (C) 2018 Haiping Chen
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

using BotSharp.Algorithm;
using BotSharp.Algorithm.Bayes;
using BotSharp.Algorithm.Estimators;
using BotSharp.Algorithm.Extensions;
using BotSharp.Algorithm.Features;
using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// This is a simple (naive) classification method based on Bayes rule. 
    /// It relies on a very simple representation of the document (called the bag of words representation)
    /// This technique works well for topic classification; 
    /// say we have a set of academic papers, and we want to classify them into different topics (computer science, biology, mathematics).
    /// Naive Bayes is best for Less training data
    /// </summary>
    public class NaiveBayesClassifier : IClassifier
    {
        private List<FeaturesDistribution> featuresDist;

        private List<Probability> labelDist;

        public void Train(List<FeaturesWithLabel> featureSets, ClassifyOptions options)
        {
            labelDist = featureSets.GroupBy(x => x.Label)
                .Select(x => new Probability
                {
                    Value = x.Key,
                    Freq = x.Count()
                })
                .ToList();

            var fNames = new List<string>();

            featureSets.ForEach(fs => fNames.AddRange(fs.Features.Select(x => x.Name)));
            fNames = fNames.OrderBy(x => x).Distinct().ToList();

            var featureValues = new Dictionary<string, List<Feature>>();

            for (int i = 0; i < featureSets.Count; i++)
            {
                var fs = featureSets[i];
                featureValues[fs.Label] = new List<Feature>();

                fNames.ForEach(fn =>
                {
                    Feature feature = null;
                    for (int j = 0; j < fs.Features.Count; j++)
                    {
                        if (fs.Features[j].Name == fn)
                        {
                            feature = fs.Features[j];
                            break;
                        }
                    }

                    var fv = new Feature(fn, feature == null ? "False" : feature.Value);
                    featureValues[fs.Label].Add(fv);
                });
            }

            featuresDist = new List<FeaturesDistribution>();

            labelDist.Select(x => x.Value).ToList().ForEach(label =>
            {
                var fSets = featureValues[label];

                fNames.ForEach(fName =>
                {
                    var fsv = fSets.Where(fs => fs.Name == fName)
                        .GroupBy(fs => fs.Value)
                        .Select(fs => new Probability
                        {
                            Value = fs.Key,
                            Freq = fs.Count()
                        })
                        .OrderBy(fs => fs.Value)
                        .ToList();

                    featuresDist.Add(new FeaturesDistribution
                    {
                        Label = label,
                        FeatureName = fName,
                        FeatureValues = fsv
                    });
                });
            });
        }

        public List<Tuple<string, double>> Classify(List<Feature> features, ClassifyOptions options)
        {
            // calculate prop
            var nb = new NaiveBayes<Lidstone>();
            nb.LabelDist = labelDist;
            nb.FeaturesDist = featuresDist;

            Parallel.ForEach(labelDist, (lf) => lf.Prob = nb.PosteriorProb(lf.Value, features));

            // add log
            double[] logs = labelDist.Select(x => x.Prob).ToArray();

            var sumLogs = logs.Reduce((log1, next) =>
            {
                double min = log1;
                if (next < log1)
                {
                    min = next;
                }

                return min + Math.Log(Math.Pow(2, log1 - min) + Math.Pow(2, next - min), 2);
            });

            labelDist.ForEach(d => d.Prob -= sumLogs);

            return labelDist.Select(x => new Tuple<string, double>(x.Value, x.Prob)).ToList();
        }
    }

    public class FeaturesWithLabel
    {
        public List<Feature> Features { get; set; }
        public string Label { get; set; }
        public FeaturesWithLabel()
        {
            this.Features = new List<Feature>();
        }
    }
}
