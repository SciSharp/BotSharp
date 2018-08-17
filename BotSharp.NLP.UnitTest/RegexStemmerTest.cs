using BotSharp.NLP.Stem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class RegexStemmerTest
    {
        [TestMethod]
        public void StemInDefault()
        {
            var stemmer = new StemmerFactory<RegexStemmer>(new StemOptions
            {
                Pattern = RegexStemmer.DEFAULT
            }, SupportedLanguage.English);

            var stem = stemmer.Stem("doing");

            Assert.IsTrue(stem == "do");
        }
    }
}
