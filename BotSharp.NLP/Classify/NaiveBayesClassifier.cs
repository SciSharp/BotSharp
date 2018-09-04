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
    /// </summary>
    public class NaiveBayesClassifier : IClassifier
    {
        public void Classify(LabeledFeatureSet featureSet, ClassifyOptions options)
        {
            throw new NotImplementedException();
        }

        public void Train(List<LabeledFeatureSet> featureSets, ClassifyOptions options)
        {
            var labelFreqDist = featureSets.GroupBy(x => x.Label)
                .Select(x => new { Label = x.Key, Count = x.Count() })
                .ToList();

            var fNames = featureSets[0].Features.Select(x => x.Name).ToList();

            // combine all features.
            var allFeatureValues = new List<Feature>();
            featureSets.ForEach(fs => fNames.ForEach(fName => allFeatureValues.Add(new Feature(fName, fs.Features.First(x => x.Name == fName).Value))));

            var featureValues = fNames.Select(fn => new
            {
                Name = fn,
                Values = allFeatureValues.Where(x => x.Name == fn).Select(x => x.Value).Distinct().ToList()
            }).ToList();

            var allFeatureFreq = new List<FeatureFrequencyDistribution>();
            featureSets.ForEach(fs =>
            {
                fs.Features.ForEach(f =>
                {
                    allFeatureFreq.Add(new FeatureFrequencyDistribution
                    {
                        Label = fs.Label,
                        FeatureName = f.Name,
                        FeatureValue = f.Value,
                        Count = 1
                    });
                });
            });

            var featureFreqDist = allFeatureFreq.GroupBy(x => new { x.Label, x.FeatureName, x.FeatureValue })
                .Select(x => new FeatureFrequencyDistribution
                {
                    Label = x.Key.Label,
                    FeatureName = x.Key.FeatureName,
                    FeatureValue = x.Key.FeatureValue,
                    Count = x.Count()
                }).ToList();

            var featureProbDist = featureFreqDist.GroupBy(x => new { x.Label, x.FeatureName })
                .Select(x => new FeatureProbabilityDistribution
                {
                    Label = x.Key.Label,
                    FeatureName = x.Key.FeatureName,
                    Count = featureFreqDist.Where(ffd => ffd.Label == x.Key.Label && ffd.FeatureName == x.Key.FeatureName).Sum(ffd => ffd.Count)
                }).ToList();
        }
    }

    public class LabeledFeatureSet
    {
        public List<Feature> Features { get; set; }
        public string Label { get; set; }
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

        public string FeatureValue { get; set; }

        public int Count { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {FeatureValue} {Count}";
        }
    }
}
