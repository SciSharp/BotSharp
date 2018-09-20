using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tag;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class NGramTaggerTest : TestEssential
    {
        [TestMethod]
        public void UniGramInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);
            tokenizer.GetTokenizer<RegexTokenizer>();

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory(new TagOptions
            {
                CorpusDir = Configuration.GetValue<String>("BotSharp.NLP:dataDir"),
                NGram = 1,
                Tag = "NN"
            }, SupportedLanguage.English);

            tagger.GetTagger<NGramTagger>();

            var watch = Stopwatch.StartNew();
            tagger.Tag(new Sentence { Words = tokens });
            watch.Stop();
            var elapsedMs1 = watch.ElapsedMilliseconds;

            Assert.IsTrue(tokens[0].Pos == "NNP");
            Assert.IsTrue(tokens[1].Pos == "IN");
            Assert.IsTrue(tokens[2].Pos == "DT");
            Assert.IsTrue(tokens[3].Pos == "NNP");

            // test if model is loaded repeatly.
            watch = Stopwatch.StartNew();
            tagger.Tag(new Sentence { Words = tokens });
            watch.Stop();
            var elapsedMs2 = watch.ElapsedMilliseconds;

            Assert.IsTrue(elapsedMs1 > elapsedMs2 * 100);
        }

        [TestMethod]
        public void BiGramInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);
            tokenizer.GetTokenizer<RegexTokenizer>();

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory(new TagOptions
            {
                CorpusDir = Configuration.GetValue<String>("BotSharp.NLP:dataDir"),
                NGram = 2,
                Tag = "NN"
            }, SupportedLanguage.English);

            tagger.GetTagger<NGramTagger>();

            tagger.Tag(new Sentence { Words = tokens });

            Assert.IsTrue(tokens[0].Pos == "NNP");
            Assert.IsTrue(tokens[1].Pos == "IN");
            Assert.IsTrue(tokens[2].Pos == "DT");
            Assert.IsTrue(tokens[3].Pos == "NNP");
        }

        [TestMethod]
        public void TriGramInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);
            tokenizer.GetTokenizer<RegexTokenizer>();

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory(new TagOptions
            {
                CorpusDir = Configuration.GetValue<String>("BotSharp.NLP:dataDir"),
                NGram = 3,
                Tag = "NN"
            }, SupportedLanguage.English);

            tagger.GetTagger<NGramTagger>();

            tagger.Tag(new Sentence { Words = tokens });

            Assert.IsTrue(tokens[0].Pos == "NNP");
            Assert.IsTrue(tokens[1].Pos == "IN");
            Assert.IsTrue(tokens[2].Pos == "DT");
            Assert.IsTrue(tokens[3].Pos == "NNP");
        }
    }
}
