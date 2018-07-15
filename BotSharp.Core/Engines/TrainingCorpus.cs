using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TrainingCorpus
    {
        public List<TrainingIntentExpression<TrainingIntentExpressionPart>> UserSays { get; set; }

        public List<TrainingEntity> Entities { get; set; }
    }
}
