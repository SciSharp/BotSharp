using Bot.Rasa.Intents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Agents
{
    public class AgentResponse
    {
        public AgentResponseIntent Intent { get; set; }

        public String Text { get; set; }
    }

    public class AgentResponseIntent
    {
        public String Name { get; set; }

        public Decimal Confidence { get; set; }
    }
}
