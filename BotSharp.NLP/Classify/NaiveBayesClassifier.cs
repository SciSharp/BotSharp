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
using BotSharp.Algorithm.Extensions;
using BotSharp.Algorithm.Formulas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// This is a simple (naive) classification method based on Bayes rule. 
    /// It relies on a very simple representation of the document (called the bag of words representation)
    /// This technique works well for topic classification; 
    /// say we have a set of academic papers, and we want to classify them into different topics (computer science, biology, mathematics).
    /// Naive Bayes is best for Less training data
    /// P(X, Y) = P(Y|X)P(X) = P(X|Y)P(Y) => P(Y|X) = P(Y)P(X|Y)/P(X)
    /// Y is label, X is features.
    /// </summary>
    public class NaiveBayesClassifier : IClassifier
    {
        private List<FeatureFrequencyDistribution> featureDist;

        private List<Probability> labelDist;

        public void Train(List<LabeledFeatureSet> featureSets, ClassifyOptions options)
        {
            labelDist = featureSets.GroupBy(x => x.Label)
                .Select(x => new Probability
                {
                    Value = x.Key,
                    Freq = x.Count()
                })
                .ToList();

            var fNames = featureSets[0].Features.Select(x => x.Name)
                .OrderBy(x => x)
                .ToList();

            // combine all features.
            var allFeatureValues = new List<Feature>();
            featureSets.ForEach(fs => fNames.ForEach(fName => allFeatureValues.Add(new Feature(fName, fs.Features.First(x => x.Name == fName).Value))));

            var featureValues = fNames.Select(fn => new
            {
                Name = fn,
                Values = allFeatureValues.Where(x => x.Name == fn).Select(x => x.Value).Distinct().ToList()
            }).ToList();

            featureDist = new List<FeatureFrequencyDistribution>();

            labelDist.Select(x => x.Value).ToList().ForEach(label =>
            {
                var fSets = featureSets.Where(x => x.Label == label);
                fNames.ForEach(fName =>
                {
                    var fsv = fSets.Select(fs => fs.Features.First(f => f.Name == fName))
                        .GroupBy(f => f.Value)
                        .Select(f => new Probability
                        {
                            Value = f.Key,
                            Freq = f.Count()
                        })
                        .OrderBy(f => f.Value)
                        .ToList();

                    featureDist.Add(new FeatureFrequencyDistribution
                    {
                        Label = label,
                        FeatureName = fName,
                        FeatureValues = fsv
                    });
                });
            });
        }

        public List<Tuple<string, double>> Classify(LabeledFeatureSet featureSet, ClassifyOptions options)
        {
            var estimator = new Lidstone();

            labelDist.ForEach(lf =>
            {
                // prior probability
                lf.Prob = estimator.Log2Prob(labelDist, lf.Value);

                // post probability P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
                featureSet.Features.ForEach(f =>
                {
                    var fv = featureDist.Find(x => x.Label == lf.Value && x.FeatureName == f.Name).FeatureValues;
                    lf.Prob += estimator.Log2Prob(fv, f.Value);
                });
            });

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

    public class LabeledFeatureSet
    {
        public List<Feature> Features { get; set; }
        public string Label { get; set; }
        public LabeledFeatureSet()
        {
            this.Features = new List<Feature>();
        }
    }

    public class Feature
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Feature(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class FeatureProbabilityDistribution
    {
        public string Label { get; set; }

        public string FeatureName { get; set; }

        public int Count { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {Count}";
        }
    }

    public class FeatureFrequencyDistribution
    {
        public string Label { get; set; }

        public string FeatureName { get; set; }

        public List<Probability> FeatureValues { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {FeatureValues.Count}";
        }
    }
}
