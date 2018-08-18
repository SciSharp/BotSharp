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
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            _contextMapping = new List<NGramFreq>();

            for (int idx = 0; idx < options.Corpus.Count; idx++)
            {
                var sent = options.Corpus[idx];

                for (int ngram = 1; ngram < options.NGram; ngram++)
                {
                    sent.Words.Insert(0, new Token { Text = "NIL", Pos = options.Tag, Start = (ngram - 1) * 3 });
                }

                int pos = options.NGram - 1;
                for (pos = 1; pos < sent.Words.Count; pos++)
                {
                    var freq = new NGramFreq
                    {
                        PrecedingTokens = new List<Token> { sent.Words[pos - 1] },
                        Token = sent.Words[pos],
                        Count = 0
                    };

                    _contextMapping.Add(freq);
                }
            }

            /*var results = (from c in cache
                           group c by c.Item1 into g
                           select new { g.Key, Count = g.Count() }).ToList();*/
        }
        
        private class NGramFreq
        {
            /// <summary>
            /// Tokens prior current token
            /// </summary>
            public List<Token> PrecedingTokens { get; set; }

            /// <summary>
            /// Current token tag
            /// </summary>
            public Token Token { get; set; }

            /// <summary>
            /// Occurence frequency
            /// </summary>
            public int Count { get; set; }

            public string Context
            {
                get
                {
                    return $"{PrecedingTokens.First().Pos} {Token.Text} {Token.Pos}";
                }
            }
        }
    }
}
