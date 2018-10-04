using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class TrainingCorpus
    {
        public List<TrainingIntentExpression<TrainingIntentExpressionPart>> UserSays { get; set; }

        /// <summary>
        /// User custom entities
        /// </summary>
        public List<TrainingEntity> Entities { get; set; }
    }
}
