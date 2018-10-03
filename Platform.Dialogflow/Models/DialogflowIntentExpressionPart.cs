using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Models
{
    public class DialogflowIntentExpressionPart
    {
        public String Text { get; set; }
        public String Alias { get; set; }
        public String Meta { get; set; }
        public int Start { get; set; }
        public Boolean UserDefined { get; set; }
    }
}
