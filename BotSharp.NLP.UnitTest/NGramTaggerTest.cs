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
        public void TagInCoNLL2000()
        {
            // tokenization
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("How are you doing?");

            // get training corpus
            string corpusDir = Environment.GetEnvironmentVariable("BOTSHARP_CORPUS_PATH", EnvironmentVariableTarget.User);
            var sentences = new CoNLLReader()
                .Read(new ReaderOptions
                {
                    DataDir = Path.Combine(corpusDir, "CoNLL"),
                    FileName = "conll2000_chunking_train.txt"
                });

            // start tag
            var tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 2,
                Tag = "NN",
                Corpus = sentences
            }, SupportedLanguage.English);

            tagger.Tag(new Sentence { Words = tokens });
        }
    }
}
