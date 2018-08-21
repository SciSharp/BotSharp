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

        private Regex _regex;

        public string Stem(string word, StemOptions options)
        {
            _regex = new Regex(options.Pattern);

            var match = _regex.Matches(word).Cast<Match>().FirstOrDefault();
        
            return match == null ? word : word.Substring(0, match.Index);
        }
    }
}
