using BotSharp.NLP.Featuring;
using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.UnitTest.Featuring
{
    [TestClass]
    public class CountFeatureExtractorTest : TestEssential
    {
        [TestMethod]
        public void TestVectorizer()
        {
            var tokenizer = new TokenizerFactory(new TokenizationOptions { }, SupportedLanguage.English);
            tokenizer.GetTokenizer<TreebankTokenizer>();

            var extractor = new CountFeatureExtractor();
            extractor.Sentences = tokenizer.Tokenize(Corpus());
            extractor.Vectorize();

            var vectors = Vectors();

            for (int i = 0; i < extractor.Sentences.Count; i++)
            {
                var sentence = extractor.Sentences[i];

                for(int j = 0; j < extractor.Features.Count; j++)
                {
                    var word = sentence.Words.Find(w => w.Lemma == extractor.Features[j]);

                    if(word != null)
                    {
                        Assert.IsTrue(word.Vector == vectors[i][j]);
                    }
                }
            }
        }

        public List<String> Corpus()
        {
            return new List<string>
            {
                "This is the first document.",
                "This document is the second document.",
                "And this is the third one.",
                "Is this the first document?"
            };
        }

        public int[][] Vectors()
        {
            return new int[4][]
            {
                new int []{ 0, 1, 1, 1, 0, 0, 1, 0, 1 },
                new int []{ 0, 2, 0, 1, 0, 1, 1, 0, 1 },
                new int []{ 1, 0, 0, 1, 1, 0, 1, 1, 1 },
                new int []{ 0, 1, 1, 1, 0, 0, 1, 0, 1 }
            };
        }
    }
}
