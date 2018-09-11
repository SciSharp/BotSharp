using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Txt2Vec
{
    public class Shrink
    {
        public void Run(string strModelFileName, string strNewModelFileName, string strDictFileName)
        {
            string strLine = null;

            //Load lexical dictionary
            // Logger.WriteLine("Load lexical dictionary...");
            StreamReader sr = new StreamReader(strDictFileName);
            HashSet<string> setTerm = new HashSet<string>();
            while ((strLine = sr.ReadLine()) != null)
            {
                string[] items = strLine.Split('\t');
                setTerm.Add(items[0]);
            }
            sr.Close();


            //Load raw model
            // Logger.WriteLine("Loading raw model...");
            sr = new StreamReader(strModelFileName);
            BinaryReader br = new BinaryReader(sr.BaseStream);

            int words = br.ReadInt32();
            int size = br.ReadInt32();
            int vqSize = br.ReadInt32();

            // Logger.WriteLine("vocabulary size: {0}, vector size: {1}, VQ size: {2}", words, size, vqSize);
            if (vqSize != 0)
            {
                // Logger.WriteLine(Logger.Level.err, "Currently, we don't support to shrink vector quantization model.");
                return;
            }

            Dictionary<string, int> vocab = new Dictionary<string, int>();
            Dictionary<int, string> rev_vocab = new Dictionary<int, string>();
            List<string> termList = new List<string>();
            double []M = new double[words * size];

            int newwords = 0;
            for (int b = 0; b < words; b++)
            {
                string strTerm = br.ReadString();
                if (setTerm.Contains(strTerm) == true)
                {
                    termList.Add(strTerm);
                    for (int a = 0; a < size; a++)
                    {
                        M[a + newwords * size] = br.ReadSingle();
                    }
                    newwords++;
                }
                else
                {
                    //Skip the vectors of this word
                    for (int a = 0; a < size; a++)
                    {
                        br.ReadSingle();
                    }
                }
            }
            sr.Close();

            //Save the shrinked model
            // Logger.WriteLine("Saving shrinked model...");
            StreamWriter sw = new StreamWriter(strNewModelFileName);
            BinaryWriter bw = new BinaryWriter(sw.BaseStream);

            bw.Write(newwords);
            bw.Write(size);
            bw.Write(vqSize);

            for (int i = 0; i < newwords; i++)
            {
                bw.Write(termList[i]);
                for (int j = 0; j < size; j++)
                {
                    bw.Write((float)M[j + i * size]);
                }
            }
            sw.Close();
        }
    }
}
