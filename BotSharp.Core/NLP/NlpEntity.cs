using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class NlpEntity
    {
        /// <summary>
        /// Entity label
        /// </summary>
        public string Entity { get; set; }

        public string Value { get; set; }

        public int Start { get; set; }

        public decimal Confidence { get; set; }

        public int End { get; set; }
    }
}
