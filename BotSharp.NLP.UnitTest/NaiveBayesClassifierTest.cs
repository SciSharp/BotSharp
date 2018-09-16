using BotSharp.NLP.Classify;
using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.Extensions;
using BotSharp.NLP.Txt2Vec;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class NaiveBayesClassifierTest : TestEssential
    {
        [TestMethod]
        public void CookingTest()
        {
            var reader = new FasttextDataReader();
            var sentences = reader.Read(new ReaderOptions
            {
                DataDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "cooking.stackexchange"),
                FileName = "cooking.stackexchange.txt"
            });

            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions { }, SupportedLanguage.English);     
            var newSentences = tokenizer.Tokenize(sentences.Select(x => x.Text).ToList());
            for(int i = 0; i < newSentences.Count; i++)
            {
                newSentences[i].Label = sentences[i].Label;
            }
            sentences = newSentences.ToList();
            
            sentences.Shuffle();

            var options = new ClassifyOptions
            {
                ModelFilePath = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "cooking.stackexchange", "nb.model"),
                TrainingCorpusDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "cooking.stackexchange"),
                Dimension = 100
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, SentenceFeatureExtractor>(options, SupportedLanguage.English);
            
            var dataset = sentences.Split(0.7M);
            classifier.Train(dataset.Item1);

            int correct = 0;
            int total = 0;
            dataset.Item2.ForEach(td =>
            {
                var classes = classifier.Classify(td);
                if (td.Label == classes[0].Item1)
                {
                    correct++;
                }
                total++;
            });

            var accuracy = (float)correct / total;

            Assert.IsTrue(accuracy > 0.5);
        }

        [TestMethod]
        public void GenderTest()
        {
            var options = new ClassifyOptions
            {
                TrainingCorpusDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Gender")
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, WordFeatureExtractor>(options, SupportedLanguage.English);

            var corpus = GetLabeledCorpus(options);

            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            corpus.ForEach(x => x.Words = tokenizer.Tokenize(x.Text));

            classifier.Train(corpus);
            string text = "Bridget";
            classifier.Classify(new Sentence { Text = text, Words = tokenizer.Tokenize(text) });

            corpus.Shuffle();
            var trainingData = corpus.Skip(2000).ToList();
            classifier.Train(trainingData);

            var testData = corpus.Take(2000).ToList();
            int correct = 0;
            testData.ForEach(td =>
            {
                var classes = classifier.Classify(td);
                if(td.Label == classes[0].Item1)
                {
                    correct++;
                }
            });

            var accuracy = (float)correct / testData.Count;
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

        [TestMethod]
        public void SpotifyTest()
        {
            var reader = new FasttextDataReader();
            var sentences = reader.Read(new ReaderOptions
            {
                DataDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "spotify"),
                FileName = "spotify.txt"
            });

            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions { }, SupportedLanguage.English);
            var newSentences = tokenizer.Tokenize(sentences.Select(x => x.Text).ToList());
            for (int i = 0; i < newSentences.Count; i++)
            {
                newSentences[i].Label = sentences[i].Label;
            }
            sentences = newSentences.ToList();

            sentences.Shuffle();

            var options = new ClassifyOptions
            {
                ModelFilePath = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "spotify", "nb.model"),
                TrainingCorpusDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "spotify")
            };
            var classifier = new ClassifierFactory<NaiveBayesClassifier, SentenceFeatureExtractor>(options, SupportedLanguage.English);

            var dataset = sentences.Split(0.7M);
            classifier.Train(dataset.Item1);

            int correct = 0;
            int total = 0;
            dataset.Item2.ForEach(td =>
            {
                var classes = classifier.Classify(td);
                if (td.Label == classes[0].Item1)
                {
                    correct++;
                }
                total++;
            });

            var accuracy = (float)correct / total;

            Assert.IsTrue(accuracy > 0.6);
        }

    }
}
