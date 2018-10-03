using BotSharp.NLP.Txt2Vec;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Txt2Vec;

namespace BotSharp.NLP.UnitTest.Vector
{
    public class Word2VecTest
    {
        [TestClass]
        public class OneHotEncodingTest : TestEssential
        {
            [TestMethod]
            public void Word2VecTest()
            {
                string sentence = "stop this song";
                List<string> words = sentence.Split(' ').ToList();
                Args args = new Args();
                args.ModelFile = @"C:\Users\bpeng\Desktop\BoloReborn\Txt2VecDemo\wordvec_enu.bin";
                VectorGenerator vg = new VectorGenerator(args);

                vg.Distance(words);
            }
        }
    }
}
