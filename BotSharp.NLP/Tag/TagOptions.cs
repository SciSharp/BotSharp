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

        /// <summary>
        /// N-Gram number
        /// </summary>
        public int NGram { get; set; }

        /// <summary>
        /// Tagged corpus used for training a model
        /// </summary>
        public List<Sentence> Corpus { get; set; }
    }
}
