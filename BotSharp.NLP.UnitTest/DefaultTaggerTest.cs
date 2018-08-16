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
            var sentences = new CoNLLReader()
                .Read(new ReaderOptions
                {
                    DataDir = AppDomain.CurrentDomain.BaseDirectory,
                    FileName = "conll2000_chunking_train"
                });

            var tagger = new TaggerFactory<DefaultTagger>();

            // tokenize
            
            tagger.Tag(null,
                new TagOptions
                {
                    
                });
        }
    }
}
