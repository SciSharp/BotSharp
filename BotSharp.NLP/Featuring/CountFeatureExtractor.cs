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

using Bigtree.Algorithm.Matrix;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.NLP.Featuring
{
    /// <summary>
    /// Convert a collection of text documents to a matrix of token counts
    /// </summary>
    public class CountFeatureExtractor : IFeatureExtractor
    {
        public int Dimension { get; set; }
        public List<Sentence> Sentences { get; set; }

        public List<Tuple<string, int>> Dictionary { get; set; }
        public List<string> Features { get; set; }
        public Shape Shape { get; set; }

        public void Vectorize()
        {
            CalculateDictionary();

            int[][] vec = new int[Sentences.Count][];

            Sentences.ForEach(s =>
            {
                s.Vector = new double[Features.Count];
                for (int i = 0; i < Features.Count; i++)
                {
                    s.Vector[i] = s.Words.Count(w => w.Lemma == Features[i]);
                }

                for (int i = 0; i < s.Words.Count; i++)
                {
                    var dic = Dictionary.Find(x => x.Item1 == s.Words[i].Lemma);
                    if(dic != null)
                    {
                        s.Words[i].Vector = s.Words.Count(w => w.Lemma == dic.Item1);
                    }
                }
            });
        }

        private void CalculateDictionary()
        {
            if (Dictionary == null)
            {
                List<Token> allWords = new List<Token>();

                Sentences.ForEach(s =>
                {
                    allWords.AddRange(s.Words);
                });

                Features = allWords.Where(w => w.IsAlpha).Select(x => x.Lemma).Distinct().OrderBy(x => x).ToList();

                Dictionary = new List<Tuple<string, int>>();

                allWords.Select(x => x.Lemma)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
                    .ForEach(word =>
                    {
                        Dictionary.Add(new Tuple<string, int>(word, allWords.Count(x => x.Lemma == word)));
                    });
            }
        }
    }
}
