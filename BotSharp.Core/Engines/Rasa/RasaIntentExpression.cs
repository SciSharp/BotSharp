using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class RasaIntentExpression
    {
        public RasaIntentExpression()
        {
        }

        public String Text { get; set; }
        public String Intent { get; set; }
        public String ContextHash { get; set; }
        public List<RasaIntentExpressionPart> Entities { get; set; }
    }
}
