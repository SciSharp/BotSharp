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
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Tokenize
{
    /// <summary>
    /// Regular-Expression Tokenizers
    /// </summary>
    public class RegexTokenizer : ITokenizer
    {
        /// <summary>
        /// Tokenize a text into a sequence of alphabetic and non-alphabetic characters
        /// </summary>
        public const string WORD_PUNC = @"[^\w\s]+|\w+";

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

        public List<Token> Tokenize(string sentence, TokenizationOptions options)
        {
            string pattern = options.Pattern;
            if (options.SpecialWords != null)
            {
                options.SpecialWords.ForEach(r =>
                {
                    sentence = Regex.Replace(sentence, r, " " + r);
                });

                pattern = String.Join("|", options.SpecialWords) + "|" + pattern;
            }

            _regex = new Regex(pattern);

            var matches = _regex.Matches(sentence).Cast<Match>().ToArray();

            options.IsGap = new string[] { WHITE_SPACE, BLANK_LINE }.Contains(pattern);

            if (options.IsGap)
            {
                int pos = 0;
                var tokens = new Token[matches.Length + 1];

                for (int span = 0; span <= matches.Length; span++)
                {
                    var token = new Token
                    {
                        Text = (span == matches.Length) ? sentence.Substring(pos) : sentence.Substring(pos, matches[span].Index - pos),
                        Start = pos
                    };

                    token.Text = token.Text.Trim();

                    tokens[span] = token;

                    if (span < matches.Length)
                    {
                        pos = matches[span].Index + 1;
                    }
                }

                return tokens.ToList();
            }
            else
            {
                var m = matches.Select(x => new Token
                {
                    Text = x.Value,
                    Start = x.Index
                }).ToList();

                if(options.SpecialWords != null)
                {
                    int offset = 0;
                    m.ForEach(t =>
                    {
                        if (options.SpecialWords.Contains(t.Text))
                        {
                            offset++;
                        }

                        t.Start = t.Start - offset;
                    });
                }


                return m;
            }
        }
    }
}
