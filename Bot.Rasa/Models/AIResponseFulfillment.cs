using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    public class AIResponseFulfillment
    {
        public string Speech { get; set; }

        public List<object> Messages { get; set; }
    }
}
