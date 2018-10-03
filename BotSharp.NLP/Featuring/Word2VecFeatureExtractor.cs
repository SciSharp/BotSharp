using System;
using System.Collections.Generic;
using System.Text;
using Bigtree.Algorithm.Matrix;
using Txt2Vec;

namespace BotSharp.NLP.Featuring
{
    public class Word2VecFeatureExtractor : IFeatureExtractor
    {
        public int Dimension { get; set; }
        public List<Sentence> Sentences { get; set; }
        public List<Tuple<string, int>> Dictionary { get; set; }
        public List<string> Features { get; set; }
        public Shape Shape { get; set; }
        public VectorGenerator Vg { get; set; }
        public int SentenceVectorSize { get; set; }

        public Word2VecFeatureExtractor()
        {
            Args args = new Args();
            args.ModelFile = "C:\\Users\\bpeng\\Desktop\\BoloReborn\\BotSharp\\BotSharp.WebHost\\App_Data\\wordvec_enu.bin";
            this.Vg = new VectorGenerator(args);
            this.SentenceVectorSize = this.Vg.Model.VectorSize * MaxSentenceTokenCounts();
        }

        public void Vectorize()
        {
            Sentences.ForEach(s => {
                Vec sentenceVec = new Vec();
                s.Words.ForEach(word => {
                    Vec wordVec = Vg.Word2Vec(word.Text);
                    sentenceVec.VecNodes.AddRange(wordVec.VecNodes);
                });
                while (sentenceVec.VecNodes.Count != SentenceVectorSize)
                {
                    sentenceVec.VecNodes.Add(0);
                }
                s.Vector = sentenceVec.VecNodes.ToArray();
            });
        }

        private int MaxSentenceTokenCounts()
        {
            int maxCount = 0;
            Sentences.ForEach(s=> {
                if (s.Words.Count > maxCount)
                {
                    maxCount = s.Words.Count;
                }
            });

            return maxCount;
        }
    }
}
