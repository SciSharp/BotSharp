using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.Entities
{
    public class NlpEntity
    {
        /// <summary>
        /// What module or algorithm name
        /// </summary>
        public string Extrator { get; set; }

        /// <summary>
        /// Entity label
        /// </summary>
        public string Entity { get; set; }

        public string Value { get; set; }

        public int Start { get; set; }

        public decimal Confidence { get; set; }

        public int End
        {
            get
            {
                return Start + Value.Length - 1;
            }
        }
    }
}
