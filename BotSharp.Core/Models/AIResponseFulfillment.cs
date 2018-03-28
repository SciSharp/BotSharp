using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class AIResponseFulfillment
    {
        public string Speech { get; set; }

        public List<Object> Messages { get; set; }
    }
}
