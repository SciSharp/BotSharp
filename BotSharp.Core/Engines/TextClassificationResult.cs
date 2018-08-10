using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TextClassificationResult
    {
        public String Label { get; set; }

        public Decimal Confidence { get; set; }
    }
}
