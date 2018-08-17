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
        public Dictionary<string, string> ContextMapping { get; set; }

        public void Tag(Sentence sentence, TagOptions options)
        {
            // need training to generate model
            if(ContextMapping == null)
            {
                var cache = new List<Tuple<String, String>>();
                var contextTag = new List<Tuple<String, String, int>>();

                ContextMapping = new Dictionary<string, string>();

                options.Corpus.ForEach(sent =>
                {
                    // Supplementary place
                    for (int ngram = 1; ngram < options.NGram; ngram++)
                    {
                        sent.Words.Insert(0, new Token { Text = "NIL", Pos = options.Tag, Start = (ngram - 1) * 3 });
                    }

                    int pos = options.NGram - 1;
                    for(pos = 1; pos < sent.Words.Count; pos++)
                    {
                        Token pre = sent.Words[pos - 1];
                        Token cur = sent.Words[pos];

                        cache.Add(new Tuple<string, string>($"{pre.Pos} {cur.Text}", cur.Pos));// Dictionary.Add($"{pre.Pos} {cur.Text}", cur.Pos);
                    }
                });

                var results = (from c in cache
                               group c by c.Item1 into g
                               select new { g.Key, Count = g.Count() }).ToList();

                results.ForEach(x =>
                {
                    int count = cache.Count(c => c.Item1 == x.Key);
                });
            }
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            throw new NotImplementedException();
        }
        
        private class NGramFreq
        {
            public string Key { get; set; }
        }
    }
}
