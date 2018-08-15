using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tokenize
{
    /// <summary>
    /// A tokenizer is a component used for dividing text intotokens. 
    /// A tokenizer is language specific and takes into account the peculiarities of the language, e.g. don’t in English is tokenized as two tokens.
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Language
        /// </summary>
        SupportedLanguage Lang { get; set; }

        /// <summary>
        /// Tokenize
        /// </summary>
        /// <param name="text">input</param>
        /// <param name="options">Options such as: regex expression</param>
        /// <returns></returns>
        Token[] Tokenize(string text, TokenizationOptions options);
    }
}
