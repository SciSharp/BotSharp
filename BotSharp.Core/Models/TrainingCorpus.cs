using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class TrainingCorpus
    {
        public TrainingCorpus()
        {
            UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>();
            Entities = new List<TrainingEntity>();
        }

        public List<TrainingIntentExpression<TrainingIntentExpressionPart>> UserSays { get; set; }

        /// <summary>
        /// User custom entities
        /// </summary>
        public List<TrainingEntity> Entities { get; set; }
    }
}
