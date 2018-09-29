using BotSharp.Core.Engines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class IntentExampleModel
    {
        public string userSays { get; set; }

        public List<ArticulateTrainingIntentExpressionPart> Entities { get; set; }
    }

    public class ArticulateTrainingIntentExpressionPart : TrainingIntentExpressionPart
    {
        public string EntityId { get; set; }
    }
}
