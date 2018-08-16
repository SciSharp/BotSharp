using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Stem
{
    /// <summary>
    /// BotSharp Stemmer Factory
    /// In linguistic morphology and information retrieval, 
    /// stemming is the process of reducing inflected (or sometimes derived) words to their word stem, 
    /// base or root form—generally a written word form.
    /// </summary>
    /// <typeparam name="IStem"></typeparam>
    public class StemmerFactory<IStem> where IStem : IStemmer, new()
    {
        private IStem _stemmer;

        public StemmerFactory()
        {
            _stemmer = new IStem();
        }

        public string Stem(string word, StemOptions options)
        {
            return _stemmer.Stem(word, options);
        }
    }
}
