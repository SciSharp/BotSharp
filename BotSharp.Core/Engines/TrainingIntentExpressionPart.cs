using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TrainingIntentExpressionPart
    {
        public int Start { get; set; }
        public int End { get { return Start + Value.Length; } }
        public String Value { get; set; }
        public String Entity { get; set; }
    }
}
