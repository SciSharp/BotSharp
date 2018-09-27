using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tokenize
{
    public abstract class TokenizerBase
    {
        protected void CorrectTokenPosition(string sentence, List<Token> tokens)
        {
            int startPos = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                token.Start = sentence.IndexOf(token.Text, startPos);

                startPos = token.End;
            }
        }
    }
}
