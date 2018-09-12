/// <summary>
/// Copyright (c) 2018 Bo Peng
/// 
/// Permission is hereby granted, free of charge, to any person obtaining
/// a copy of this software and associated documentation files (the
/// "Software"), to deal in the Software without restriction, including
/// without limitation the rights to use, copy, modify, merge, publish,
/// distribute, sublicense, and/or sell copies of the Software, and to
/// permit persons to whom the Software is furnished to do so, subject to
/// the following conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// </summary>
/// 
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Txt2Vec
{
    public class TFIDF
    {
        public List<Sentence> Sentences { get; set; }

        public List<string> Words { get; set; }

        public void Encode(Sentence sentence)
        {
            InitDictionary();

            // var featureSets = Sentences.Select(x => new Tuple<string, double[]>(x.Label, x.Vector)).ToList();

            var labelDist = Sentences.Select(x => x.Label).Distinct().ToList();

            labelDist.ForEach(label =>
            {
                // https://zhuanlan.zhihu.com/p/31197209
                // calculate TF
                // all words in the article
                List<string> words = new List<string>();
                Sentences.Where(x => x.Label == label).ToList().ForEach(sent =>
                {
                    words.AddRange(sent.Words.Select(w => w.Text));
                });

                List<Tuple<string, double>> tfs = new List<Tuple<string, double>>();
                words.Distinct().ToList().ForEach(w =>
                {
                    // TF
                    int c1 = words.Count(x => x == w);
                    double tf = (c1 + 1.0) / words.Count();

                    // IDF
                    var sents = Sentences.Where(s => s.Words.Select(x => x.Text).Contains(w)).ToList();
                    double idf = Math.Log(Sentences.Count / (sents.Count() + 1.0));

                    tfs.Add(new Tuple<string, double>(w, tf * idf));
                });

                tfs = tfs.OrderByDescending(x => x.Item2).Take(words.Count / 10).ToList();
            });

            

            sentence.Words.ForEach(w =>
            {
                int index = Words.IndexOf(w.Text.ToLower());
            });
        }

        public List<string> EncodeAll()
        {
            InitDictionary();

            Sentences.ForEach(sent => Encode(sent));
            //Parallel.ForEach(Sentences, sent => Encode(sent));

            return Words;
        }

        private List<string> InitDictionary()
        {
            if (Words == null)
            {
                Words = new List<string>();
                Sentences.ForEach(x =>
                {
                    Words.AddRange(x.Words.Where(w => w.IsAlpha).Select(w => w.Text.ToLower()));
                });
                Words = Words.Distinct().OrderBy(x => x).ToList();
            }

            return Words;
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
    }
}
