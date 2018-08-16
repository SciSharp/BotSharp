using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class RegexpTokenizerTest
    {
        [TestMethod]
        public void TokenizeInWhiteSpace()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>();

            var tokens = tokenizer.Tokenize("Chop into pieces, isn't it?",
                new TokenizationOptions
                {
                    Pattern = RegexTokenizer.WHITE_SPACE
                });

            Assert.IsTrue(tokens[0].Offset == 0);
            Assert.IsTrue(tokens[0].Text == "Chop");

            Assert.IsTrue(tokens[1].Offset == 5);
            Assert.IsTrue(tokens[1].Text == "into");

            Assert.IsTrue(tokens[2].Offset == 10);
            Assert.IsTrue(tokens[2].Text == "pieces,");

            Assert.IsTrue(tokens[3].Offset == 18);
            Assert.IsTrue(tokens[3].Text == "isn't");

            Assert.IsTrue(tokens[4].Offset == 24);
            Assert.IsTrue(tokens[4].Text == "it?");
        }

        [TestMethod]
        public void TokenizeInWordPunctuation()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>();

            var tokens = tokenizer.Tokenize("Chop into pieces, isn't it?",
                new TokenizationOptions
                {
                    Pattern = RegexTokenizer.WORD_PUNC
                });

            Assert.IsTrue(tokens[0].Offset == 0);
            Assert.IsTrue(tokens[0].Text == "Chop");

            Assert.IsTrue(tokens[1].Offset == 5);
            Assert.IsTrue(tokens[1].Text == "into");

            Assert.IsTrue(tokens[2].Offset == 10);
            Assert.IsTrue(tokens[2].Text == "pieces");

            Assert.IsTrue(tokens[3].Offset == 16);
            Assert.IsTrue(tokens[3].Text == ",");

            Assert.IsTrue(tokens[4].Offset == 18);
            Assert.IsTrue(tokens[4].Text == "isn");

            Assert.IsTrue(tokens[5].Offset == 21);
            Assert.IsTrue(tokens[5].Text == "'");

            Assert.IsTrue(tokens[6].Offset == 22);
            Assert.IsTrue(tokens[6].Text == "t");

            Assert.IsTrue(tokens[7].Offset == 24);
            Assert.IsTrue(tokens[7].Text == "it");

            Assert.IsTrue(tokens[8].Offset == 26);
            Assert.IsTrue(tokens[8].Text == "?");
        }

        [TestMethod]
        public void TokenizeInBlankLine()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>();

            var tokens = tokenizer.Tokenize(@"Chop into pieces, 

isn't

it?",
                new TokenizationOptions
                {
                    Pattern = RegexTokenizer.BLANK_LINE
                });

            Assert.IsTrue(tokens[0].Offset == 0);
            Assert.IsTrue(tokens[0].Text == "Chop into pieces,");

            Assert.IsTrue(tokens[1].Offset == 18);
            Assert.IsTrue(tokens[1].Text == "isn't");

            Assert.IsTrue(tokens[2].Offset == 28);
            Assert.IsTrue(tokens[2].Text == "it?");
        }
    }
}
