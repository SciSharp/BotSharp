using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tag;
using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class NGramTaggerTest
    {
        [TestMethod]
        public void UniGramInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 1,
                Tag = "NN",
                Corpus = GetTaggedCorpus()
            }, SupportedLanguage.English);

            tagger.Tag(new Sentence { Words = tokens });

            Assert.IsTrue(tokens[0].Pos == "NNP");
            Assert.IsTrue(tokens[1].Pos == "IN");
            Assert.IsTrue(tokens[2].Pos == "DT");
            Assert.IsTrue(tokens[3].Pos == "NNP");
        }

        [TestMethod]
        public void BiGramInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 2,
                Tag = "NN",
                Corpus = GetTaggedCorpus()
            }, SupportedLanguage.English);

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
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Chancellor of the Exchequer Nigel Lawson's restated commitment");

            // test tag
            var tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 3,
                Tag = "NN",
                Corpus = GetTaggedCorpus()
            }, SupportedLanguage.English);

            tagger.Tag(new Sentence { Words = tokens });

            Assert.IsTrue(tokens[0].Pos == "NNP");
            Assert.IsTrue(tokens[1].Pos == "IN");
            Assert.IsTrue(tokens[2].Pos == "DT");
            Assert.IsTrue(tokens[3].Pos == "NNP");
        }

        private List<Sentence> GetTaggedCorpus()
        {
            // get training corpus
            string corpusDir = Environment.GetEnvironmentVariable("BOTSHARP_CORPUS_PATH", EnvironmentVariableTarget.User);
            return new CoNLLReader()
                .Read(new ReaderOptions
                {
                    DataDir = Path.Combine(corpusDir, "CoNLL"),
                    FileName = "conll2000_chunking_train.txt"
                });
        }
    }
}
