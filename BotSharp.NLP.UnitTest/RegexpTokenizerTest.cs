using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class RegexpTokenizerTest
    {
        [TestMethod]
        public void Tokenize()
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

            Assert.IsTrue(tokens[3].Offset == 24);
            Assert.IsTrue(tokens[3].Text == "it?");
        }
    }
}
