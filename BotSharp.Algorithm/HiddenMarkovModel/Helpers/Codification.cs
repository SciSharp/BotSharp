using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Helpers
{
    public class Codification
    {
        protected int mSymbolCount = 0;
        protected Dictionary<string, int> mSymbolIdTable = new Dictionary<string, int>();
        protected List<string> mIdSymbolTable = new List<string>();

        public int SymbolCount
        {
            get { return mSymbolCount; }
        }

        public Codification(string[][] phrases)
        {
            foreach (string[] row in phrases)
            {
                Parse(row);
            }
        }

        public string[] Translate(int[] id_list)
        {
            int N = id_list.Length;
            string[] phrases = new string[N];
            for (int i = 0; i < N; ++i)
            {
                phrases[i] = Translate(id_list[i]);
            }
            return phrases;
        }

        public string[][] Translate(int[][] id_matrix)
        {
            int N1 = id_matrix.Length;
            string[][] phrases = new string[N1][];
            for (int i = 0; i < N1; ++i)
            {
                phrases[i] = Translate(id_matrix[i]);
            }
            return phrases;
        }

        public int[] Translate(string[] phrases)
        {
            int N = phrases.Length;
            int[] id_list = new int[N];
            for (int i = 0; i < N; ++i)
            {
                id_list[i] = Translate(phrases[i]);
            }

            return id_list;
        }

        public int[][] Translate(string[][] phrases)
        {
            int N1=phrases.Length;
            int[][] id_matrix = new int[N1][];
            for (int i = 0; i < N1; ++i)
            {
                id_matrix[i] = Translate(phrases[i]);
            }
            return id_matrix;
        }

        public int Translate(string word)
        {
            if(mSymbolIdTable.ContainsKey(word))
            {
                 return mSymbolIdTable[word];
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Symbol {0} is not found in the codification", word));
            }
        }

        public string Translate(int id)
        {
            if (mIdSymbolTable.Count > id)
            {
                return mIdSymbolTable[id];
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Symbol ID {0} is not found in the codification", id));
            }
        }

        public void Parse(string[] phrases)
        {
            foreach (string word in phrases)
            {
                if (!mSymbolIdTable.ContainsKey(word))
                {
                    mSymbolIdTable[word] = mSymbolCount;
                    mIdSymbolTable.Add(word);

                    mSymbolCount++; 
                }
            }
        }
    }
}
