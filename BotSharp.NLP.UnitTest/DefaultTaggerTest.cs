using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tag;
using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class DefaultTaggerTest
    {
        [TestMethod]
        public void TagInCoNLL2000()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions { }, SupportedLanguage.English);
            var tokens = tokenizer.Tokenize("How are you doing?");

            var tagger = new TaggerFactory<DefaultTagger>(new TagOptions
            {
                Tag = "NN"
            }, SupportedLanguage.English);

            tagger.Tag(new Sentence { Words = tokens });
        }
    }
}