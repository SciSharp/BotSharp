using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.AiResponse
{
    public class TextClassificationResult
    {
        public String Classifier { get; set; }

        public String Label { get; set; }

        public string Text { get; set; }

        public double Confidence { get; set; }
    }
}
