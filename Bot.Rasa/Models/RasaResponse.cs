using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    public class RasaResponse
    {
        public RasaResponseIntent Intent { get; set; }

        public String Text { get; set; }
    }

    public class RasaResponseIntent
    {
        public String Name { get; set; }

        public Decimal Confidence { get; set; }
    }
}
