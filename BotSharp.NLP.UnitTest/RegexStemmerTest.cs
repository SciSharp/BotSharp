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
            var stemmer = new StemmerFactory<RegexStemmer>();

            var stem = stemmer.Stem("doing",
                new StemOptions
                {
                    Pattern = RegexStemmer.DEFAULT
                });

            Assert.IsTrue(stem == "do");
        }
    }
}
