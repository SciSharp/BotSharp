using BotSharp.NLP.Classify;
using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Txt2Vec;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class SVMClassifierTest : TestEssential
    {
        [TestMethod]
        public void TFIDFTest()
        {
            string[] documents =
            {
                "Hello, how are you!",
                "Hi Bolo!",
                "Hey Haiping!",
                "Hello Haiping",
                "hi, how do you do?",
                "goodbye Haiping",
                "see you Bolo",
                "byebye Haiping"
            };
            /*TFIDFGenerator tfidfGenerator = new TFIDFGenerator();
            List<List<double>> weights = tfidfGenerator.TFIDFWeightVectorsForSentences(documents);*/
        }

        [TestMethod]
        public void Doc2VectorTest()
        {
            List<string> sentences = new List<string>();
            sentences.Add("The sun in the sky is bright.");
            sentences.Add("We can see the shining sun, the bright sun.");
            Args args = new Args();
            args.ModelFile = "C:\\Users\\bpeng\\Desktop\\BoloReborn\\BotSharp.NLP\\BotSharp.NLP.UnitTest\\wordvec_enu.bin";
            VectorGenerator vg = new VectorGenerator(args);
            var list = vg.Sentence2Vec(sentences);
        }

        [TestMethod]
        public void similarityTest()
        {
            List<string> sentences = new List<string>();
            sentences.Add("How's it going");
            sentences.Add("How's your day");
            sentences.Add("How's everything");
            sentences.Add("Good morning");
            sentences.Add("Good afternoon");
            sentences.Add("Good evening");
            sentences.Add("I appreciate it");
            sentences.Add("Thanks a lot");
            sentences.Add("Thank you");


            Args args = new Args();
            args.ModelFile = "C:\\Users\\bpeng\\Desktop\\BoloReborn\\BotSharp.NLP\\BotSharp.NLP.UnitTest\\wordvec_enu.bin";
            VectorGenerator vg = new VectorGenerator(args);
            var list = vg.Sentence2Vec(sentences);
            Vec vec1 = vg.SingleSentence2Vec("Good morning");
            Vec vec2 = vg.SingleSentence2Vec("How's it going");
            double score = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score);

            vec1 = vg.SingleSentence2Vec("Good morning");
            vec2 = vg.SingleSentence2Vec("How's your day");
            double score1 = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score1);

            vec1 = vg.SingleSentence2Vec("Good morning");
            vec2 = vg.SingleSentence2Vec("How's everything");
            double score2 = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score2);


            vec1 = vg.SingleSentence2Vec("Good morning");
            vec2 = vg.SingleSentence2Vec("Good afternoon");
            double score3 = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score3);

            vec1 = vg.SingleSentence2Vec("Good morning");
            vec2 = vg.SingleSentence2Vec("I appreciate");
            double score4 = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score4);

            vec1 = vg.SingleSentence2Vec("Good morning");
            vec2 = vg.SingleSentence2Vec("Thanks a lot");
            double score5 = vg.Similarity(vec1, vec2);
            Console.WriteLine("Similarity score: {0}", score5);
        }
    }
}
