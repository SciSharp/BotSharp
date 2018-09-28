using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class IntentExampleModel
    {
        public string userSays { get; set; }

        public List<ArticulateTrainingIntentExpressionPart> Entities { get; set; }
    }

    public class ArticulateTrainingIntentExpressionPart : TrainingIntentExpressionPart
    {
        public int EntityId { get; set; }
    }
}
