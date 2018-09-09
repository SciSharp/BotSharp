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

            featuresDist = new List<FeaturesDistribution>();

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
            
            labelDist.ForEach(lf => lf.Prob = nb.PosteriorProb(lf.Value, features));

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
}
