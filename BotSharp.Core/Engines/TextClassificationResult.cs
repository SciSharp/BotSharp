using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TextClassificationResult
    {
        public String Classifier { get; set; }

        public String Label { get; set; }

        public double Confidence { get; set; }
    }
}
