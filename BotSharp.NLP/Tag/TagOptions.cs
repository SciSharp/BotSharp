using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tag
{
    public class TagOptions
    {
        /// <summary>
        /// Display some stats, if requested.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Default Tag
        /// Used in DefaultTagger
        /// </summary>
        public string Tag { get; set; }
    }
}
