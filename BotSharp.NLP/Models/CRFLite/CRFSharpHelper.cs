using BotSharp.Models.CRFLite.Decoder;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Models.CRFLite
{
    public class SegToken
    {
        public int Offset;
        public int Length;
        public string Tag;
        public double Weight;
    };

    public class CRFSegOut : CRFTermOut
    {
        //Segmented token by merging raw CRF model output
        public int termTotalLength;          // the total term length in character
        public List<SegToken> tokenList;

        public int Count
        {
            get { return tokenList.Count; }
        }

        public void Clear()
        {
            termTotalLength = 0;
            tokenList.Clear();
        }

        public CRFSegOut(int max_word_num = BaseUtils.DEFAULT_CRF_MAX_WORD_NUM):
            base(max_word_num)
        {
            termTotalLength = 0;
            tokenList = new List<SegToken>();
        }
    };

}
