using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public class RasaResponseEntity : RasaIntentExpressionPart
    {
        public string Extractor { get; set; }
    }
}
