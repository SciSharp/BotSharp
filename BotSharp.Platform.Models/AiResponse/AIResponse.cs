using BotSharp.Platform.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.AiResponse
{
    public class AiResponse
    {
        public AiResponse()
        {
            Entities = new List<NlpEntity>();
        }

        public string ResolvedQuery { get; set; }

        public string Intent { get; set; }

        public string Source { get; set; }

        public double Score { get; set; }

        public List<NlpEntity> Entities { get; set; }
    }
}
