using BotSharp.NLP.Classify;
using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class NaiveBayesClassifierTest : TestEssential
    {
        [TestMethod]
        public void GenderTest()
        {
            var options = new ClassifyOptions
            {
                TrainingCorpusDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Gender")
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier>(options, SupportedLanguage.English);

            var corpus = GetLabeledCorpus(options);

            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            corpus.ForEach(x => x.Words = tokenizer.Tokenize(x.Text));

            classifier.Train(corpus);
        }

        private List<Sentence> GetLabeledCorpus(ClassifyOptions options)
        {
            var reader = new LabeledPerFileNameReader();

            var genders = new List<Sentence>();

            var female = reader.Read(new ReaderOptions
            {
                DataDir = options.TrainingCorpusDir,
                FileName = "female.txt"
            });

            genders.AddRange(female);

            var male = reader.Read(new ReaderOptions
            {
                DataDir = options.TrainingCorpusDir,
                FileName = "male.txt"
            });

            genders.AddRange(male);

            return genders;
        }
    }
}
