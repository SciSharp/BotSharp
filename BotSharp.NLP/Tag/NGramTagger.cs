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
using System.Linq;
using System.Text;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Tag
{
    /// <summary>
    /// N-Gramm taggers are based on a simple statistical algorithm: 
    /// for each token, assign the tag that is most likely for that particular token.
    /// </summary>
    public class NGramTagger : ITagger
    {
        private List<NGramFreq> _contextMapping { get; set; }

        public void Tag(Sentence sentence, TagOptions options)
        {
            // need training to generate model
            if(_contextMapping == null)
            {
                Train(options.Corpus, options);
            }

            Fill(sentence, options);

            for (int pos = options.NGram - 1; pos < sentence.Words.Count; pos++)
            {
                sentence.Words[pos].Pos = _contextMapping.FirstOrDefault(x => x.Context == GetContext(pos, sentence.Words, options))?.Tag;

                // set default tag
                if(sentence.Words[pos].Pos == null)
                {
                    sentence.Words[pos].Pos = options.Tag;
                }
            }

            for(int pos = 0; pos < options.NGram - 1; pos++)
            {
                sentence.Words.RemoveAt(0);
            }
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            var cache = new List<NGramFreq>();

            for (int idx = 0; idx < options.Corpus.Count; idx++)
            {
                var sent = options.Corpus[idx];

                Fill(sent, options);

                for (int pos = options.NGram - 1; pos < sent.Words.Count; pos++)
                {
                    var freq = new NGramFreq
                    {
                        Context = GetContext(pos, sent.Words, options),
                        Tag = sent.Words[pos].Pos,
                        Count = 1
                    };

                    cache.Add(freq);
                }
            }

            _contextMapping = (from c in cache
                               group c by new { c.Context, c.Tag } into g
                               select new NGramFreq
                               {
                                   Context = g.Key.Context,
                                   Tag = g.Key.Tag,
                                   Count = g.Count()
                               }).OrderByDescending(x => x.Count)
                               .ToList();
        }

        private string GetContext(int pos, List<Token> words, TagOptions options)
        {
            string context = words[pos].Text;
            for (int ngram = options.NGram - 1; ngram > 0; ngram--)
            {
                context = words[pos - ngram].Pos + " " + context;
            }

            return context;
        }

        private void Fill(Sentence sent, TagOptions options)
        {
            for (int ngram = 1; ngram < options.NGram; ngram++)
            {
                sent.Words.Insert(0, new Token { Text = "NIL", Pos = options.Tag, Start = (ngram - 1) * 3 });
            }
        }
        
        private class NGramFreq
        {
            /// <summary>
            /// Current token tag
            /// </summary>
            public string Tag { get; set; }

            /// <summary>
            /// Occurence frequency
            /// </summary>
            public int Count { get; set; }

            public string Context { get; set; }
        }
    }
}
