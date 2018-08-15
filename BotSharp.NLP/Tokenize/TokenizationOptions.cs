using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tokenize
{
    public class TokenizationOptions
    {
        /// <summary>
        /// Regex pattern
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// True if this tokenizer's pattern should be used to find separators between tokens; 
        /// False if this tokenizer's pattern should be used to find the tokens themselves.
        /// </summary>
        public bool IsGap { get; set; }
    }
}
