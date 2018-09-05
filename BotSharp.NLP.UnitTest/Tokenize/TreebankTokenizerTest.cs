using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.UnitTest.Tokenize
{
    [TestClass]
    public class TreebankTokenizerTest
    {
        [TestMethod]
        public void ReplaceStartingQuoting()
        {
            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions
            {
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("(\"«Hello World.");
        }

        [TestMethod]
        public void ReplacePunctuation()
        {
            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions
            {
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Hello World...");
        }

        [TestMethod]
        public void ReplaceBrackets()
        {
            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions
            {
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("<Hello World.>");
        }

        [TestMethod]
        public void ReplaceConventions()
        {
            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions
            {
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("I cannot jump.");
        }
    }
}
