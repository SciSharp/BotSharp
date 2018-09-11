using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Tokenize
{
    public class Token
    {
        /// <summary>
        /// The original word text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The offset of word
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The simple part-of-speech tag.
        /// Not widely used, Tag is more general.
        /// </summary>
        public string Pos { get; set; }

        /// <summary>
        /// The detailed part-of-speech tag.
        /// https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The base form of the word.
        /// </summary>
        public string Lemma { get; set; }

        /// <summary>
        /// The word shape – capitalisation, punctuation, digits.
        /// </summary>
        public string Shape { get; set; }

        /// <summary>
        /// Is the token an alpha character?
        /// </summary>
        public bool IsAlpha
        {
            get
            {
                return Regex.IsMatch(Text, @"^[a-zA-Z]+$");
            }
        }

        /// <summary>
        /// Is the token part of a stop list, i.e. the most common words of the language?
        /// </summary>
        public bool IsStop { get; set; }

        public int End
        {
            get
            {
                return Start + Text.Length;
            }
        }

        public override string ToString()
        {
            return $"{Text} {Start} {Pos}";
        }
    }
}
