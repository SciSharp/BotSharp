using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tokenize
{
    /// <summary>
    /// BotSharp Tokenizer Factory
    /// Tokenizers divide strings into lists of substrings.
    /// The particular tokenizer requires implement interface 
    /// models to be installed.BotSharp.NLP also provides a simpler, regular-expression based tokenizer, which splits text on whitespace and punctuation.
    /// </summary>
    public class TokenizerFactory<ITokenize> where ITokenize : ITokenizer, new()
    {
        private ITokenize _tokenizer;

        public TokenizerFactory()
        {
            _tokenizer = new ITokenize();
        }

        public Token[] Tokenize(string sentence, TokenizationOptions options)
        {
            return _tokenizer.Tokenize(sentence, options);
        }
    }
}
