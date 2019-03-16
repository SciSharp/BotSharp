using BotSharp.Core.Engines;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Articulate.Models
{
    public class IntentExampleModel
    {
        public string UserSays { get; set; }

        public List<ArticulateTrainingIntentExpressionPart> Entities { get; set; }
    }

    public class ArticulateTrainingIntentExpressionPart : TrainingIntentExpressionPart
    {
        public string EntityId { get; set; }
    }
}
