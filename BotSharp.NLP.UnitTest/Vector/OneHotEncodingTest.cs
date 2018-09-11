using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tokenize;
using BotSharp.NLP.Txt2Vec;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.NLP.UnitTest.Vector
{
    [TestClass]
    public class OneHotEncodingTest : TestEssential
    {
        [TestMethod]
        public void OneHotTest()
        {
            var reader = new FasttextDataReader();
            var sentences = reader.Read(new ReaderOptions
            {
                DataDir = Path.Combine(Configuration.GetValue<String>("MachineLearning:dataDir"), "Text Classification", "cooking.stackexchange"),
                FileName = "cooking.stackexchange.txt"
            });

            var tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions { }, SupportedLanguage.English);
            var newSentences = tokenizer.Tokenize(sentences.Select(x => x.Text).ToList());
            for (int i = 0; i < newSentences.Count; i++)
            {
                newSentences[i].Label = sentences[i].Label;
            }
            sentences = newSentences.ToList();

            var encoder = new OneHotEncoder();
            encoder.Sentences = sentences;
            encoder.EncodeAll();
        }
    }
}
