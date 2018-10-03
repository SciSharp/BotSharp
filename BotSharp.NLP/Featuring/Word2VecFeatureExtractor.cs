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

        }

        public void Vectorize(List<string> features)
        {
            Init();

            Sentences.ForEach(s => {
                List<string> wordLemmas = new List<string>();
                s.Words.ForEach(word => {
                    if (features.Contains(word.Lemma))
                    {
                        wordLemmas.Add(word.Lemma);
                    }
                });
                Vec sentenceVec = Vg.Sent2Vec(wordLemmas);

                s.Vector = sentenceVec.VecNodes.ToArray();
            });


        }

        private void Init()
        {
            if(Vg == null)
            {
                Args args = new Args();
                args.ModelFile = @"C:\Users\bpeng\Desktop\BoloReborn\Txt2VecDemo\wordvec_enu.bin";
                Vg = new VectorGenerator(args);
                SentenceVectorSize = this.Vg.Model.VectorSize * MaxSentenceTokenCounts();
                Features = new List<string>();
                for (int i = 0; i < SentenceVectorSize; i++)
                {
                    Features.Add($"f-{i}");
                }
            }
        }

        private int MaxSentenceTokenCounts()
        {
            return 1;
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
