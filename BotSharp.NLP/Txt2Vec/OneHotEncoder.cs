using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.NLP.Txt2Vec
{
    /// <summary>
    /// A one hot encoding is a representation of categorical variables as binary vectors. 
    /// Each integer value is represented as a binary vector that is all zero values except the index of the integer, which is marked with a 1.
    /// </summary>
    public class OneHotEncoder
    {
        public List<Sentence> Sentences { get; set; }

        private List<string> words;

        public void Encode(Sentence sentence)
        {
            InitDictionary();

            var vector = words.Select(x => 0D).ToArray();

            sentence.Words.ForEach(w =>
            {
                int index = words.IndexOf(w.Text.ToLower());
                if(index > 0)
                {
                    vector[index] = 1;
                }
            });

            sentence.Vector = vector;
        }

        public void EncodeAll()
        {
            InitDictionary();
            Parallel.ForEach(Sentences, sent =>
            {
                Encode(sent);
            });
        }

        private void InitDictionary()
        {
            if (words == null)
            {
                words = new List<string>();
                Sentences.ForEach(x =>
                {
                    words.AddRange(x.Words.Where(w => w.IsAlpha).Select(w => w.Text.ToLower()));
                });
                words = words.Distinct().OrderBy(x => x).ToList();
            }
        }
    }
}
