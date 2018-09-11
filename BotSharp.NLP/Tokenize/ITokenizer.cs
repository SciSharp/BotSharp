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
        /// Tokenize
        /// </summary>
        /// <param name="sentence">input sentence</param>
        /// <param name="options">Options such as: regex expression</param>
        /// <returns></returns>
        List<Token> Tokenize(string sentence, TokenizationOptions options);
    }
}
