using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.MachineLearning.NLP
{
    public class NlpToken
    {
        public string Text { get; set; }
        public int Offset { get; set; }
        public string Pos { get; set; }
        public string Tag { get; set; }
        public string Lemma { get; set; }
        public int End
        {
            get
            {
                return Offset + Text.Length;
            }
        }
    }
}
