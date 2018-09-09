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
    /// Penn Treebank Tokenizer
    /// The Treebank tokenizer uses regular expressions to tokenize text as in Penn Treebank.
    /// This implementation is a port of the tokenizer sed script written by Robert McIntyre 
    /// and available at ftp://ftp.cis.upenn.edu/pub/treebank/public_html/tokenizer.sed,
    /// or reference ftp://ftp.cis.upenn.edu/pub/treebank/public_html/tokenization.html.
    /// </summary>
    public class TreebankTokenizer : ITokenizer
    {
        private List<Tuple<String, String>> STARTING_QUOTES = new List<Tuple<string, string>>();
        private List<Tuple<String, String>> PUNCTUATION = new List<Tuple<string, string>>();
        private List<Tuple<String, String>> PARENS_BRACKETS = new List<Tuple<string, string>>();
        private List<Tuple<String, String>> CONVERT_PARENTHESES = new List<Tuple<string, string>>();
        private List<Tuple<String, String>> ENDING_QUOTES = new List<Tuple<string, string>>();
        private List<Tuple<String, String>> CONVENTIONS = new List<Tuple<string, string>>();

        public TreebankTokenizer()
        {
            Init();
        }

        public List<Token> Tokenize(string sentence, TokenizationOptions options)
        {
            string text = sentence;

            // starting quoting replace
            STARTING_QUOTES.ForEach(x =>
            {
                text = Regex.Replace(text, x.Item1, x.Item2);
            });

            // replace PUNCTUATION
            PUNCTUATION.ForEach(x =>
            {
                text = Regex.Replace(text, x.Item1, x.Item2);
            });

            // Handles parentheses.
            PARENS_BRACKETS.ForEach(x =>
            {
                text = Regex.Replace(text, x.Item1, x.Item2);
            });

            // convert parentheses
            if (options.ConvertParentheses)
            {
                CONVERT_PARENTHESES.ForEach(x =>
                {
                    text = Regex.Replace(text, x.Item1, x.Item2);
                });
            }

            // Handles repeated dash.
            text = Regex.Replace(text, "(-{2,})", " $1 ").Trim();

            // replace ending quotes
            ENDING_QUOTES.ForEach(x =>
            {
                text = Regex.Replace(text, x.Item1, x.Item2);
            });

            // replace ending quotes
            CONVENTIONS.ForEach(x =>
            {
                text = Regex.Replace(text, x.Item1, x.Item2);
            });

            // remove duplicated spaces
            text = Regex.Replace(text, "\\s+", " ") + " ";

            // split
            int pos = 0;

            var tokens = Regex.Matches(text, "\\s")
                .Cast<Match>()
                .Select(x => {

                    var token = new Token
                    {
                        Start = pos,
                        Text = text.Substring(pos, x.Index - pos)
                    };

                    pos = x.Index + 1;

                    return token;

                }).ToList();

            // correct token position
            CorrectTokenPosition(sentence, tokens);

            return tokens;
        }

        private void CorrectTokenPosition(string sentence, List<Token> tokens)
        {
            int startPos = 0;

            for(int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                token.Start = sentence.IndexOf(token.Text, startPos);

                startPos = token.End;
            }
        }

        private void Init()
        {
            STARTING_QUOTES.Add(new Tuple<string, string>(@"([«“‘„]|[`]+)", " $1 "));
            STARTING_QUOTES.Add(new Tuple<string, string>("^\"", "``"));
            STARTING_QUOTES.Add(new Tuple<string, string>(@"(``)", " $1 "));
            STARTING_QUOTES.Add(new Tuple<string, string>("([ ([{<])(\" | '{2})", "$1 `` "));

            PUNCTUATION.Add(new Tuple<string, string>(@"([^\.])(\.)([\]\)}>" + "\"" + @"\\'»”’ ]*)\s*$", "$1 $2 $3 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"([:,])([^\d])", " $1 $2"));
            PUNCTUATION.Add(new Tuple<string, string>(@"([:,])$", " $1 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"(\.\.\.)", " $1 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"([;@#$%&])", " $1 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"([^\.])(\.)([\]\)}>" + "\"" + @"']*)\s*$", "$1 $2 $3 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"([?!])", " $1 "));
            PUNCTUATION.Add(new Tuple<string, string>(@"([^'])' ", "$1 ' "));

            PARENS_BRACKETS.Add(new Tuple<string, string>(@"([\]\[\(\)\{\}\<\>])", " $1 "));

            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\(", "-LRB-"));
            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\)", "-RRB-"));
            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\[", "-LSB-"));
            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\]", "-RRB-"));
            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\{", "-LCB-"));
            CONVERT_PARENTHESES.Add(new Tuple<string, string>(@"\}", "-RCB-"));

            ENDING_QUOTES.Add(new Tuple<string, string>(@"([»”’])", " $1 "));
            ENDING_QUOTES.Add(new Tuple<string, string>("\"", " '' "));
            ENDING_QUOTES.Add(new Tuple<string, string>(@"(\S)(\'\')", "$1 $2 "));
            ENDING_QUOTES.Add(new Tuple<string, string>(@"('[sS]|'[mM]|'[dD]|') ", " $1 "));
            ENDING_QUOTES.Add(new Tuple<string, string>(@"('ll|'LL|'re|'RE|'ve|'VE|n't|N'T) ", " $1 "));

            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(can)(?#X)(not)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(d)(?#X)('ye)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(gim)(?#X)(me)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(gon)(?#X)(na)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(got)(?#X)(ta)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(lem)(?#X)(me)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(mor)(?#X)('n)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i)\b(wan)(?#X)(na)\s", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i) ('t)(?#X)(is)\b", "$1 $2 "));
            CONVENTIONS.Add(new Tuple<string, string>(@"(?i) ('t)(?#X)(was)\b", "$1 $2 "));
        }
    }
}
