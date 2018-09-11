/*
 * BotSharp.Algorithm
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

using BotSharp.Algorithm.Estimators;
using BotSharp.Algorithm.Features;
using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Bayes
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Bayes%27_theorem
    /// </summary>
    public class MultinomiaNaiveBayes
    {
        public List<Probability> LabelDist { get; set; }

        public List<Tuple<string, double[]>> FeatureSet { get; set; }

        public double Alpha { get; set; }

        /// <summary>
        /// prior probability
        /// </summary>
        /// <param name="Y"></param>
        /// <returns></returns>
        public double CalPriorProb(string Y)
        {
            int N = FeatureSet.Count;
            int k = LabelDist.Count;
            int Nyk = LabelDist.First(x => x.Value == Y).Freq;

            return (Nyk + Alpha) / (N + k * Alpha);
        }

        /// <summary>
        /// calculate posterior probability P(Y|X)
        /// X is feature set, Y is label
        /// P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
        /// P(X, Y) = P(Y|X)P(X) = P(X|Y)P(Y) => P(Y|X) = P(Y)P(X|Y)/P(X)
        /// </summary>
        public double PosteriorProb(string Y, double[] features, double priorProb)
        {
            Alpha = 0.5;

            int featureCount = features.Length;
            
            double postProb = priorProb;

            // posterior probability P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
            var featuresIfY = FeatureSet.Where(fd => fd.Item1 == Y).ToList();
            var matrix = ConstructMatrix(featuresIfY);

            // loop features
            for (int x = 0; x < featureCount; x++)
            {
                int freq = 0;
                for (int y = 0; y < featuresIfY.Count; y++)
                {
                    if(matrix[y, x] == features[x])
                    {
                        freq++;
                    }
                }

                int Nyk = featuresIfY.Count;
                int n = featureCount;
                int Nykx = freq;

                postProb += Math.Log((Nykx + Alpha) / (Nyk + n * Alpha));
            }

            return Math.Pow(2, postProb);
        }

        private double[,] ConstructMatrix(List<Tuple<string, double[]>> featuresIfY)
        {
            var featureCount = featuresIfY[0].Item2.Length;

            double[,] matrix = new double[featuresIfY.Count, featureCount];
            for (int y = 0; y < featuresIfY.Count; y++)
            {
                for (int x = 0; x < featureCount; x++)
                {
                    matrix[y, x] = featuresIfY[y].Item2[x];
                }
            }

            return matrix;
        }
    }
}
