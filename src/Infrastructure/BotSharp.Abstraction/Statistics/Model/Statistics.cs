using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Abstraction.Statistics.Model
{
    public class Statistics
    {
        public string Id { get; set; } = string.Empty;
        public int ConversationCount { get; set; }
        public DateTime UpdatedDateTime { get; set; }
    }
}
