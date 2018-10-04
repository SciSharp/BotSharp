using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
//using AdvUtils;

namespace Txt2Vec
{
    public class VectorGenerator
    {
        public Model Model { get; set; }
        // Txt2Vec.Model model = new Txt2Vec.Model();
        Dictionary<string, Vec> dict = new Dictionary<string, Vec>();

        public VectorGenerator(Args args)
        {
            this.Model = new Txt2Vec.Model();
            bool bTxtFormat = false;
            string strModelFileName = args.ModelFile;

            if (strModelFileName == null)
            {
                Console.Write("Failed: must to set the model file name");
                throw new IOException();
            }
            if (System.IO.File.Exists(strModelFileName) == false)
            {
                Console.Write("Failed: model file {0} isn't existed.", strModelFileName);
                throw new IOException();
            }

            this.Model.LoadModel(strModelFileName, bTxtFormat);
        }

        public List<Vec> Sentence2Vec(List<string> sentences, WeightingScheme weightingScheme = WeightingScheme.AVG)
        {
            // Inplementing TF-IDF
            // TFIDFGenerator tfidfGenerator = new TFIDFGenerator();
            List<List<double>> weights = null;// tfidfGenerator.TFIDFWeightVectorsForSentences(sentences.ToArray());

            List<List<Vec>> matixList = new List<List<Vec>>();
            List<Vec> sentenceVectorList = new List<Vec>();
            sentences.ForEach (sentence=>{
                //List<Vec> sentenceVectorList = new List<Vec>();
                //string[] words = sentence.Split(' ');
                //foreach (string word in words)
                //{
                //    Vec vec = Word2Vec(word.ToLower());
                //    sentenceVectorList.Add(vec);
                //}
                //matixList.Add(sentenceVectorList);
            });

            return sentenceVectorList;
            /*
            List<Vec> vectorList = new List<Vec>();
            // Traverse each sentence
            for (int i = 0; i < sentences.Count; i++)
            {
                Vec sentenceVector = null;
                List<Vec> curVecList = matixList[i];
                if (weightingScheme == WeightingScheme.TFIDF)
                {   
                    // Get this sentence 
                    List<double> weight = weights[i];
                    sentenceVector = TFIDFMultiply(curVecList, weight);
                }
                if (weightingScheme == WeightingScheme.AVG)
                {
                    int dim = curVecList[0].VecNodes.Count;
                    sentenceVector = new Vec(); 
                    double nodeTotalValue;
                    for (int k = 0; k < dim; k++)
                    {
                        nodeTotalValue = 0;
                        for (int j = 0; j < curVecList.Count; j++)
                        {
                            Vec curWordVec = curVecList[j];
                            double curNodeVal = curWordVec.VecNodes[k];
                            nodeTotalValue += curNodeVal;
                        }
                        sentenceVector.VecNodes.Add(nodeTotalValue / dim);
                        
                    }
                    
                }
                vectorList.Add(sentenceVector);
            }
            for (int i = 0; i < vectorList.Count; i++)
            {
                if (this.dict.ContainsKey(sentences[i]))
                {
                    continue;
                }
                else
                {
                    this.dict.Add(sentences[i], vectorList[i]);
                }
                
            }
            return vectorList;
            */
        }

        public Vec SingleSentence2Vec(string sentence, WeightingScheme weightingScheme = WeightingScheme.AVG)
        {

            Vec sentenceVector = new Vec();
            List<Vec> sentenceVectorList = new List<Vec>();
            string[] words = sentence.Split(' ');
            foreach (string word in words)
            {
                Vec vec = Word2Vec(word.ToLower());
                sentenceVectorList.Add(vec);
            }
            if (weightingScheme == WeightingScheme.AVG)
            {
                int dim = sentenceVectorList[0].VecNodes.Count;
                double nodeTotalValue;
                for (int k = 0; k < dim; k++)
                {
                    nodeTotalValue = 0;
                    for (int j = 0; j < sentenceVectorList.Count; j++)
                    {
                        Vec curWordVec = sentenceVectorList[j];
                        double curNodeVal = curWordVec.VecNodes[k];
                        nodeTotalValue += curNodeVal;
                    }
                    sentenceVector.VecNodes.Add(nodeTotalValue / dim);
                }

            }
            return sentenceVector;
        }

        public Vec TFIDFMultiply(List<Vec> curVecList, List<double> weight)
        {
            int dim = curVecList[0].VecNodes.Count;
            int sentenceWordsCount = curVecList.Count;
            Vec res = new Vec();
            for (int k = 0; k < dim; k++)
            {
                double nodeTotalValue = 0;
                for (int i = 0; i < curVecList.Count; i++)
                {
                    Vec curWordVec = curVecList[i];
                    double curNodeVal = curWordVec.VecNodes[k];
                    double curWeight = weight[i];
                    nodeTotalValue += curNodeVal * curWeight;
                }
                res.VecNodes.Add(nodeTotalValue / sentenceWordsCount);
            }

            return res;
        }

        public Vec Word2Vec(string word)
        {
            Vec vec= new Vec();

            Txt2Vec.Decoder decoder = new Txt2Vec.Decoder(Model);
            string[] termList = new string[1];
            termList[0] = word;
            vec.VecNodes = decoder.ToVector(termList).ToList();

            return vec;
        }

        public Vec Sent2Vec(List<string> words)
        {
            Vec vec = new Vec();

            Txt2Vec.Decoder decoder = new Txt2Vec.Decoder(Model);
            string[] termList = words.ToArray();
            vec.VecNodes = decoder.ToVector(termList).ToList();

            return vec;
        }

        public void Distance(List<string> words)
        {

            Txt2Vec.Decoder decoder = new Txt2Vec.Decoder(Model);
            words.ForEach(word=> {
                Console.WriteLine($"current word: {word}");
                List<Result> sysnonyms = decoder.Distance(word);
                sysnonyms.ForEach(s=> {
                    Console.WriteLine($"{s.strTerm}: {s.score}");
                });
            });
        }

        public double Similarity(Vec vec1, Vec vec2)
        {
            double score = 0;
            for (int i = 0; i < Model.VectorSize; i++)
            {
                score += vec1.VecNodes[i] * vec2.VecNodes[i];
            }

            return score;
        }
    }

    public class Vec
    {
        public List<double> VecNodes { get; set; }

        public Vec()
        {
            VecNodes = new List<double>();
        }
    }

    public class Args
    {
        public string TxtModel { get; set; }
        public string ModelFile { get; set; }
        public int MaxWord { get; set; }
    }

    public enum WeightingScheme
    {
        AVG,
        TFIDF
    }
}
