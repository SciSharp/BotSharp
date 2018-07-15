using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TrainingIntentExpression<TPart> where TPart : TrainingIntentExpressionPart
    {
        public String Text { get; set; }
        public String Intent { get; set; }

        [JsonIgnore]
        public String ContextHash { get; set; }

        public List<TPart> Entities { get; set; }
    }
}
