using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class RasaIntentExpressionPart
    {
        public int Start { get; set; }
        public int End { get; set; }
        public String Value { get; set; }
        public String Entity { get; set; }
    }
}
