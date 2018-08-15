using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Tokenize
{
    public class RegexTokenizer : ITokenizer
    {
        public SupportedLanguage Lang { get; set; }

        /// <summary>
        /// Tokenize a text into a sequence of alphabetic and non-alphabetic characters
        /// </summary>
        public const string WORD_PUNC = @"\w+|[^\w\s]+";

        /// <summary>
        /// Tokenize a string, treating any sequence of blank lines as a delimiter.
        /// Blank lines are defined as lines containing no characters, except for space or tab characters.
        /// options.IsGap = true
        /// </summary>
        public const string BLANK_LINE = @"\s*\n\s*\n\s*";

        /// <summary>
        /// Tokenize a string on whitespace (space, tab, newline).
        /// In general, users should use the string ``split()`` method instead.
        /// options.IsGap = true
        /// </summary>
        public const string WHITE_SPACE = @"\s+";

        private Regex _regex;

        public Token[] Tokenize(string text, TokenizationOptions options)
        {
            _regex = new Regex(options.Pattern);

            var matches = _regex.Matches(text).Cast<Match>().ToArray();

            options.IsGap = new string[] { WHITE_SPACE, BLANK_LINE }.Contains(options.Pattern);

            if (options.IsGap)
            {
                int pos = 0;
                int span = 0;

                var tokens = matches.Select(x =>
                {
                    var token = new Token
                    {
                        Text = (span == matches.Length - 1) ? text.Substring(pos) : text.Substring(pos, x.Index - pos),
                        Offset = pos
                    };

                    pos = x.Index + 1;

                    if (span == matches.Length - 1)
                    {

                    }
                    
                    span++;

                    return token;
                }).ToArray();

                return tokens;
            }
            else
            {
                return matches.Select(x => new Token
                {
                    Text = x.Value,
                    Offset = x.Index
                }).ToArray();
            }

        }
    }
}
