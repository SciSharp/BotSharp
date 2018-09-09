using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP
{
    public class Sentence
    {
        public List<Token> Words { get; set; }

        public String Label { get; set; }

        public String Text { get; set; }
    }
}
