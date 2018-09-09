using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP
{
    public class Sentence
    {
        public List<Token> Words { get; set; }

        /// <summary>
        /// Allow multiple classification
        /// </summary>
        public List<String> Labels { get; set; }

        public String Text { get; set; }
    }
}
