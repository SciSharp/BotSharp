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

        public List<string> Words { get; set; }

        public void Encode(Sentence sentence)
        {
            InitDictionary();

            var vector = Words.Select(x => 0D).ToArray();

            sentence.Words.ForEach(w =>
            {
                int index = Words.IndexOf(w.Text.ToLower());
                if(index > 0)
                {
                    vector[index] = 1;
                }
            });

            sentence.Vector = vector;
        }

        public List<string> EncodeAll()
        {
            InitDictionary();
            
            Sentences.ForEach(sent => Encode(sent));
            //Parallel.ForEach(Sentences, sent => Encode(sent));

            return Words;
        }

        private List<string> InitDictionary()
        {
            if (Words == null)
            {
                Words = new List<string>();
                Sentences.ForEach(x =>
                {
                    Words.AddRange(x.Words.Where(w => w.IsAlpha).Select(w => w.Text.ToLower()));
                });
                Words = Words.Distinct().OrderBy(x => x).ToList();
            }

            return Words;
        }
    }
}
