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
                int index = Words.IndexOf(w.Lemma.ToLower());
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
                Words = "shuffle,pause,resume,next,stop,previous,continue,mode,repeat,back,music,play,enough,off,them,playlist,skip,restart,favourites,on,add,go,again,turn,save,my,station,favourite,start,by,playing,please,now,running,move,gym,yoga,backward,one,favorites,mark,as,remember,fave,what,forward,me,and,could,once,more,can".Split(',').ToList();
            }

            return Words;
        }
    }
}
