using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class AIResponseCustomPayload : AIResponseMessageBase
    {
        public string Task { get; set; }

        public Object Body { get; set; }
    }
}
