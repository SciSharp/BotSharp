using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Stem
{
    /// <summary>
    /// Stemmer is used to remove morphological affixes from words, leaving only the word stem.
    /// Stemming algorithms aim to remove those affixes leaving only the stem of the word.
    /// IStemmer defines a standard interface for stemmers.
    /// </summary>
    public interface IStemmer
    {
        /// <summary>
        /// Strip affixes from the token and return the stem.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        string Stem(string word, StemOptions options);
    }
}
