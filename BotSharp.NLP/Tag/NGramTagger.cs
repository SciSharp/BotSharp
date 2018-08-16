using System;
using System.Collections.Generic;
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
        public SupportedLanguage Lang { get; set; }

        public void Tag(Sentence sentence, TagOptions options)
        {
            throw new NotImplementedException();
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
