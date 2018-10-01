using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class TrainingIntentExpression<TPart> where TPart : TrainingIntentExpressionPart
    {
        public String Text { get; set; }
        public String Intent { get; set; }

        public String ContextHash { get; set; }

        public List<TPart> Entities { get; set; }
    }
}
