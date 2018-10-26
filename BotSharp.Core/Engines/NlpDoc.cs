using BotSharp.Core.Abstractions;
using CherubNLP.Tokenize;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class NlpDoc
    {
        public INlpPredict Tokenizer { get; set; }
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
