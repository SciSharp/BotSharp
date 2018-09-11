using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Txt2Vec
{
    public enum TermOperation { ADD, SUB };

    public class TermOP
    {
        public string strTerm;
        public TermOperation operation;
    }

    public class Result : IComparable<Result>
    {
        public string strTerm;
        public double score;

        public Result()
        {
            strTerm = null;
            score = -1;
        }

        int IComparable<Result>.CompareTo(Result other)
        {
            return other.score.CompareTo(score);
        }
    }

    public class Decoder
    {
        int BLOCK_N = 16;
        Model model;
        object locker = new object();

        ParallelOptions parallelOption;
        public Decoder(Model m)
        {
            parallelOption = new ParallelOptions();
            model = m;
        }

        public double[] ToVector(string[] termList, int N = 40)
        {
            List<TermOP> termOPList = new List<TermOP>();
            foreach (string term in termList)
            {
                TermOP termOP = new TermOP();
                termOP.strTerm = term;
                termOP.operation = TermOperation.ADD;

                termOPList.Add(termOP);
            }

            double[] vec = GetVector(termOPList);
            return vec;
        }

        public double[] GetVector(List<TermOP> termList)
        {
            double[] vec = new double[model.VectorSize];

            //Calculate input terms' vector
            for (int b = 0; b < termList.Count; b++)
            {
                Term term = model.GetTerm(termList[b].strTerm);
                if (term == null)
                {
                    continue;
                }

                if (termList[b].operation == TermOperation.ADD)
                {
                    for (int a = 0; a < model.VectorSize; a++)
                    {
                        vec[a] += term.vector[a];
                    }
                }
                else if (termList[b].operation == TermOperation.SUB)
                {
                    for (int a = 0; a < model.VectorSize; a++)
                    {
                        vec[a] -= term.vector[a];
                    }
                }
            }
            return vec;
        }

        private List<TermOP> GenerateTermOP(string[] termList)
        {
            List<TermOP> termOPList = new List<TermOP>();
            foreach (string term in termList)
            {
                TermOP termOP = new TermOP();
                termOP.strTerm = term;
                termOP.operation = TermOperation.ADD;

                termOPList.Add(termOP);
            }

            return termOPList;
        }

        public double Similarity(string[] tokens1, string[] tokens2)
        {
            double score = 0;
            List<TermOP> termOPList1 = GenerateTermOP(tokens1);
            List<TermOP> termOPList2 = GenerateTermOP(tokens2);
            double[] vec1 = GetVector(termOPList1);
            double[] vec2 = GetVector(termOPList2);

            //Cosine distance
            for (int i = 0; i < model.VectorSize; i++)
            {
                score += vec1[i] * vec2[i];
            }

            return score;
        }

        public List<Result> Distance(string strTerm, int N = 40)
        {
            string[] termList = new string[1];
            termList[0] = strTerm;

            return Distance(termList, N);
        }

        //N is the number of closest words that will be shown
        public List<Result> Distance(string[] termList, int N = 40)
        {
            List<TermOP> termOPList = new List<TermOP>();
            foreach (string term in termList)
            {
                TermOP termOP = new TermOP();
                termOP.strTerm = term;
                termOP.operation = TermOperation.ADD;

                termOPList.Add(termOP);
            }

            return Distance(termOPList, N);
        }

        public List<Result> Distance(List<TermOP> termList, int N = 40)
        {
            long termCount = termList.Count;

            for (int i = 0; i < termCount; i++)
            {
                if (model.GetTerm(termList[i].strTerm) == null)
                {
                    //The term is OOV, no result
                    return null;
                }
            }

            //Calculate input terms' vector
            double[] vec = GetVector(termList);

            int candidateWordCount = model.Vocabulary.Count;
            //Calculate the distance betweens words in parallel
            int size_per_block = candidateWordCount / BLOCK_N;
            List<Result> rstList = new List<Result>();
            Parallel.For<List<Result>>(0, BLOCK_N + 1, parallelOption, () => new List<Result>(), (k, loop, subtotal) =>
            {
                for (int c = (int)(k * size_per_block); c < (k + 1) * size_per_block && c < candidateWordCount; c++)
                {
                    //Calculate the distance
                    double dist = 0;
                    for (int a = 0; a < model.VectorSize; a++)
                    {
                        dist += vec[a] * model.Vocabulary[c].vector[a];
                    }

                    //Save the result
                    Result rst = new Result();
                    rst.strTerm = model.Vocabulary[c].strTerm;
                    rst.score = dist;

                    subtotal.Add(rst);
                }

                return subtotal;
            },
           (subtotal) => // lock free accumulator
           {
               //Mereg the result from different threads
               lock (locker)
               {
                   rstList.AddRange(subtotal);
               }
           });

            //Sort the result according the distance
            rstList.Sort();

            int maxN = Math.Min(N, rstList.Count);


            return rstList.GetRange(0, maxN);
        }

    }
}
