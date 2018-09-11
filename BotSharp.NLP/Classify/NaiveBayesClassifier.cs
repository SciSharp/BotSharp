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
        private List<Probability> labelDist;

        private MultinomiaNaiveBayes nb = new MultinomiaNaiveBayes();

        /// <summary>
        /// Cache all categories' prior probability
        /// </summary>
        private Dictionary<string, double> PriorPropDictionary = new Dictionary<string, double>();

        public void Train(List<Tuple<string, double[]>> featureSets, ClassifyOptions options)
        {
            labelDist = featureSets.GroupBy(x => x.Item1)
                .Select(x => new Probability
                {
                    Value = x.Key,
                    Freq = x.Count()
                })
                .ToList();

            nb.LabelDist = labelDist;
            nb.FeatureSet = featureSets;

            // calculate prior prob
            labelDist.ForEach(l => l.Prob = nb.CalPriorProb(l.Value));

            // calculate posterior prob

        }

        public List<Tuple<string, double>> Classify(double[] features, ClassifyOptions options)
        {
            var results = new List<Tuple<string, double>>();

            // calculate prop
            labelDist.ForEach(lf =>
            {
                var prob = nb.PosteriorProb(lf.Value, features, lf.Prob);
                results.Add(new Tuple<string, double>(lf.Value, prob));
            });

            /*Parallel.ForEach(labelDist, (lf) =>
            {
                nb.Y = lf.Value;
                lf.Prob = nb.PosteriorProb();
            });*/

            return results;
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
