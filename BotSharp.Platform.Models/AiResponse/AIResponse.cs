using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.AiResponse
{
    public class AiResponse
    {
        public String ResolvedQuery { get; set; }

        public string Intent { get; set; }

        public string Source { get; set; }

        public double Score { get; set; }
    }
}
