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

//using Bigtree.Algorithm.Matrix;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Featuring
{
    public class TfIdfFeatureExtractor : IFeatureExtractor
    {
        public List<Sentence> Sentences { get; set; }

        private List<Tuple<String, double>> tfs;

        private List<string> Categories { get; set; }
        public int Dimension { get; set; }

        public List<Tuple<string, int>> Dictionary { get; set; }
        public List<string> Features { get; set; }
        //public Shape Shape { get; set; }
        public string ModelFile { get; set; }

        public void Extract(Sentence sentence)
        {

        }

        public List<string> Keywords()
        {
            if(Dimension == 0)
            {
                Dimension = Categories.Count * 3;

                if(Dimension > 300)
                {
                    Dimension = 300;
                }

                if(Dimension < 30)
                {
                    Dimension = 30;
                }
            }

            var tfs2 = tfs.OrderByDescending(x => x.Item2)
                .Select(x => x.Item1)
                .Distinct()
                .Take(Dimension)
                .OrderBy(x => x)
                .ToList();

            return tfs2;
        }

        public void CalBasedOnSentence()
        {
            Categories = Sentences.Select(x => x.Label).Distinct().ToList();

            tfs = new List<Tuple<String, double>>();

            Sentences.ForEach(sent =>
            {
                sent.Words.Where(x => x.IsAlpha).ToList().ForEach(word =>
                {
                    // TF
                    int c1 = sent.Words.Count(x => x.Lemma == word.Lemma);
                    double tf = (c1 + 1.0) / sent.Words.Count();

                    // IDF
                    var c2 = Sentences.Count(s => s.Words.Select(x => x.Lemma).Contains(word.Lemma));
                    double idf = Math.Log(Sentences.Count / (c2 + 1.0));

                    word.Vector = tf * idf;

                    tfs.Add(new Tuple<string, double>(word.Lemma, word.Vector));
                });
            });
        }

        public void CalBasedOnCategory()
        {
            tfs = new List<Tuple<String, double>>();

            Categories = Sentences.Select(x => x.Label).Distinct().ToList();

            List<Tuple<string, string>> allTextByCategory = new List<Tuple<string, string>>();

            Categories.ForEach(label =>
            {
                var allTokens = new List<Token>();
                Sentences.Where(x => x.Label == label)
                    .ToList()
                    .ForEach(s => allTokens.AddRange(s.Words));
                allTextByCategory.Add(new Tuple<string, string>(label, String.Join(" ", allTokens.Where(x => x.IsAlpha).Select(x => x.Lemma))));
            });

            Categories.ForEach(label =>
            {
                var allTokens = new List<Token>();
                Sentences.Where(x => x.Label == label)
                    .ToList()
                    .ForEach(s => allTokens.AddRange(s.Words));

                allTokens.Where(x => x.IsAlpha).Select(x => x.Lemma).Distinct()
                    .ToList()
                    .ForEach(word =>
                    {
                        // TF
                        int c1 = allTokens.Count(x => x.Lemma == word);
                        double tf = (c1 + 1.0) / allTokens.Count();

                        // IDF
                        var c2 = 0;
                        allTextByCategory.ForEach(all =>
                        {
                            if(Regex.IsMatch(all.Item2, word))
                            {
                                c2++;
                            }
                        });

                        double idf = Math.Log(Categories.Count / (c2 + 1.0));

                        tfs.Add(new Tuple<string, double>(word, tf * idf));
                    });
            });
        }

        /// <summary>
        /// Normalizes a TF*IDF array of vectors using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">List<List<double>></param>
        /// <returns>List<List<double>></returns>
        public static List<List<double>> Normalize(List<List<double>> vectors)
        {
            // Normalize the vectors using L2-Norm.
            List<List<double>> normalizedVectors = new List<List<double>>();
            foreach (var vector in vectors)
            {
                var normalized = Normalize(vector);
                normalizedVectors.Add(normalized);
            }

            return normalizedVectors;
        }

        /// <summary>
        /// Normalizes a TF*IDF vector using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors"> List<double> </param>
        /// <returns> List<double> </returns>
        public static List<double> Normalize(List<double> vector)
        {
            List<double> result = new List<double>();

            double sumSquared = 0;
            foreach (var value in vector)
            {
                sumSquared += value * value;
            }

            double SqrtSumSquared = Math.Sqrt(sumSquared);

            foreach (var value in vector)
            {
                // L2-norm: Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
                result.Add(value / SqrtSumSquared);
            }
            return result;
        }

        public void Vectorize(List<string> features)
        {
            throw new NotImplementedException();
        }
    }
}
