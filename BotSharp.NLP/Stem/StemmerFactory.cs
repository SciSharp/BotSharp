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
        private SupportedLanguage _lang { get; set; }

        private IStem _stemmer;

        private StemOptions _options;

        public StemmerFactory(StemOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            _stemmer = new IStem();
        }

        public string Stem(string word)
        {
            return _stemmer.Stem(word, _options);
        }
    }
}
