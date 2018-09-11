using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BotSharp.Models.CRFLite.Utils;

namespace Txt2Vec
{
    public class Term
    {
        public string strTerm;
        public float[] vector;
        public byte[] vectorVQ;
    }

    public class Model
    {
        private Dictionary<string, Term> term2vector;
        private List<Term> entireTermList;
        private int vectorSize;
        private double[][] codebooks;

        public List<Term> Vocabulary { get { return entireTermList; } }
        public int VectorSize { get { return vectorSize; } }
        public string[] GetAllTerms()
        {
            return term2vector.Keys.ToArray();
        }

        public Term GetTerm(string strTerm)
        {
            if (term2vector.ContainsKey(strTerm) == false)
            {
                return null;
            }

            return term2vector[strTerm];
        }

        public void LoadModel(string strFileName, bool bTextFormat)
        {
            if (bTextFormat == true)
            {
                LoadTextModel(strFileName);
            }
            else
            {
                LoadBinaryModel(strFileName);
            }
        }

        public bool DumpModel(string strFileName)
        {
            if (entireTermList == null || entireTermList.Count == 0)
            {
                return false;
            }

            StreamWriter sw = new StreamWriter(strFileName);
            foreach (Term term in entireTermList)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(term.strTerm);
                sb.Append("\t");

                foreach (double v in term.vector)
                {
                    sb.Append(v);
                    sb.Append("\t");
                }

                sw.WriteLine(sb.ToString().Trim());
            }
            sw.Close();

            return true;
        }

        public void LoadTextModel(string strFileName)
        {
            term2vector = new Dictionary<string, Term>();
            entireTermList = new List<Term>();
            vectorSize = 0;

            StreamReader sr = new StreamReader(strFileName);
            string strLine = null;
            while ((strLine = sr.ReadLine()) != null)
            {
                //the format is "word \t vector
                //eah dim of vector is splitted by \t
                Term term = new Term();
                string[] items = strLine.Split('\t');
                int vSize = items.Length - 1;
                if (vectorSize > 0 && vectorSize != vSize)
                {
                    throw new InvalidDataException(String.Format("Invalidated data : {0} . The length of vector must be fixed (current length {1} != previous length {2}).", strLine, vSize, vectorSize));
                }

                term.strTerm = items[0];
                term.vector = new float[vSize];
                for (int i = 0; i < vSize; i++)
                {
                    term.vector[i] = float.Parse(items[i + 1]);
                }

                vectorSize = vSize;
                term.vector = NormalizeVector(term.vector);
                term2vector.Add(term.strTerm, term);
                entireTermList.Add(term);
            }

            sr.Close();
        }

        public float[] GetVector(string strTerm)
        {
            if (term2vector.ContainsKey(strTerm) == true)
            {
                return term2vector[strTerm].vector;
            }

            return null;
        }

        private float[] NormalizeVector(float[] vec)
        {
            //Normalize the vector
            double len = 0;
            for (int a = 0; a < vectorSize; a++)
            {
                len += vec[a] * vec[a];
            }
            len = Math.Sqrt(len);
            for (int a = 0; a < vectorSize; a++)
            {
                vec[a] = (float)(vec[a] / len);
            }

            return vec;
        }

        public void LoadBinaryModel(string strFileName)
        {
            StreamReader sr = new StreamReader(strFileName);
            BinaryReader br = new BinaryReader(sr.BaseStream);

            //The number of words
            int words = br.ReadInt32();
            //The size of vector
            vectorSize = br.ReadInt32();
            int vqSize = br.ReadInt32();

            term2vector = new Dictionary<string, Term>();
            entireTermList = new List<Term>();

            // Logger.WriteLine("vocabulary size: {0}, vector size: {1}, VQ size: {2}", words, vectorSize, vqSize);

            codebooks = null;
            if (vqSize > 0)
            {
                //Read code books
                codebooks = new double[vectorSize][];
                for (int i = 0; i < vectorSize; i++)
                {
                    codebooks[i] = new double[vqSize];
                    for (int j = 0; j < vqSize; j++)
                    {
                        codebooks[i][j] = br.ReadDouble();
                    }
                }
            }

            for (int b = 0; b < words; b++)
            {
                Term term = new Term();
                term.strTerm = br.ReadString();
                term.vector = new float[vectorSize];
                if (codebooks != null)
                {
                    term.vectorVQ = new byte[vectorSize];
                }
                else
                {
                    term.vectorVQ = null;
                }

                for (int i = 0; i < vectorSize; i++)
                {
                    if (codebooks == null)
                    {
                        term.vector[i] = br.ReadSingle();
                    }
                    else
                    {
                        byte idx = br.ReadByte();
                        term.vector[i] = (float)codebooks[i][idx];
                        term.vectorVQ[i] = idx;
                    }
                }

                term.vector = NormalizeVector(term.vector);
                term2vector.Add(term.strTerm, term);
                entireTermList.Add(term);

            }
            sr.Close();
        }

        public static void SaveModel(string strFileName, int vocab_size, int vector_size, List<vocab_word> vocab, double[] syn)
        {
            StreamWriter fo = new StreamWriter(strFileName);
            BinaryWriter bw = new BinaryWriter(fo.BaseStream);

            // Logger.WriteLine("Saving term and vector into model file...");
            // Save the word vectors
            bw.Write(vocab_size);
            bw.Write(vector_size);
            bw.Write(0); //no VQ

            for (int i = 0; i < vocab_size; i++)
            {
                //term string
                bw.Write(vocab[i].word);

                //term vector
                for (int j = 0; j < vector_size; j++)
                {
                    bw.Write((float)(syn[i * vector_size + j]));
                }
            }

            bw.Flush();
            fo.Flush();
            fo.Close();
        }

        public bool BuildVQModel(string strFileName)
        {
            int vqSize = 256;
            if (entireTermList == null || entireTermList.Count == 0)
            {
                return false;
            }

            StreamWriter fo = new StreamWriter(strFileName);
            BinaryWriter bw = new BinaryWriter(fo.BaseStream);

            // Save the word vectors
            bw.Write(entireTermList.Count); //Vocabulary size
            bw.Write(vectorSize); //Vector size
            bw.Write(vqSize); //VQ size

            // Logger.WriteLine("vocabulary size: {0}, vector size: {1}, vq size: {2}", entireTermList.Count, vectorSize, vqSize);

            //Create word and VQ values mapping table
            Dictionary<string, List<byte>> vqResult = new Dictionary<string, List<byte>>();
            foreach (Term term in entireTermList)
            {
                vqResult.Add(term.strTerm, new List<byte>());
            }

            // Logger.WriteLine("Dims Distortion:");
            for (int i = 0; i < vectorSize; i++)
            {
                //Generate VQ values for each dimension
                VectorQuantization vq = new VectorQuantization();
                for (int j = 0; j < entireTermList.Count; j++)
                {
                    vq.Add(entireTermList[j].vector[i]);
                }
                double distortion = vq.BuildCodebook(vqSize);
                // Logger.WriteLine("Dim {0}: {1}", i, distortion);

                for (int j = 0; j < entireTermList.Count; j++)
                {
                    byte vqValue = (byte)vq.ComputeVQ(entireTermList[j].vector[i]);
                    vqResult[entireTermList[j].strTerm].Add(vqValue);
                }

                //Save VQ codebook into model file
                for (int j = 0; j < vqSize; j++)
                {
                    bw.Write(vq.CodeBook[j]);
                }
            }

            foreach (KeyValuePair<string, List<byte>> pair in vqResult)
            {
                if (pair.Value.Count != vectorSize)
                {
                    throw new Exception(String.Format("word {0} has inconsistent vector size: orginial size is {1}, vq size is {2}",
                        pair.Key, vectorSize, pair.Value.Count));
                }

                //term string
                bw.Write(pair.Key);
                //term vector
                for (int b = 0; b < pair.Value.Count; b++)
                {
                    bw.Write(pair.Value[b]);
                }
            }

            bw.Flush();
            fo.Flush();
            fo.Close();

            return true;
        }
    }
}
