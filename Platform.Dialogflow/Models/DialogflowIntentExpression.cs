using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Models
{
    public class DialogflowIntentExpression
    {
        public String Id { get; set; }
        public List<DialogflowIntentExpressionPart> Data { get; set; }
        public Boolean IsTemplate { get; set; }
    }
}
