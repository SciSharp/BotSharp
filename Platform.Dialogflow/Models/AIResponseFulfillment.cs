using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Models
{
    public class AIResponseFulfillment
    {
        public string Speech { get; set; }

        public List<Object> Messages { get; set; }
    }
}
