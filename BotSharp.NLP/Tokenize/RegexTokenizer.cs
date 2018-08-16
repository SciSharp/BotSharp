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
                var tokens = new Token[matches.Length + 1];

                for (int span = 0; span <= matches.Length; span++)
                {
                    var token = new Token
                    {
                        Text = (span == matches.Length) ? text.Substring(pos) : text.Substring(pos, matches[span].Index - pos),
                        Offset = pos
                    };

                    token.Text = token.Text.Trim();

                    tokens[span] = token;

                    if (span < matches.Length)
                    {
                        pos = matches[span].Index + 1;
                    }
                }

                return tokens.ToArray();
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
