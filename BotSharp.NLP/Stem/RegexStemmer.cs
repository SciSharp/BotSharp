using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Stem
{
    /// <summary>
    /// A stemmer that uses regular expressions to identify morphological affixes.
    /// Any substrings that match the regular expressions will be removed.
    /// </summary>
    public class RegexStemmer : IStemmer
    {
        public const string DEFAULT = "ing$|s$|e$|able$";

        public SupportedLanguage Lang { get; set; }

        private Regex _regex;

        public string Stem(string word, StemOptions options)
        {
            _regex = new Regex(options.Pattern);

            var match = _regex.Matches(word).Cast<Match>().FirstOrDefault();
        
            return match == null ? word : word.Substring(0, match.Index);
        }
    }
}
