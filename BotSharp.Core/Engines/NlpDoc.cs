using BotSharp.Models.NLP;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class NlpDoc
    {
        public List<NlpDocSentence> Sentences { get; set; }
    }

    public class NlpDocSentence
    {
        public string Text { get; set; }
        public List<Token> Tokens { get; set; }
        public List<NlpEntity> Entities { get; set; }
        public TextClassificationResult Intent { get; set; }
    }
}
