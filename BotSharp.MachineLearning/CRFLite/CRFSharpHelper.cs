using BotSharp.MachineLearning.CRFLite.Decoder;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite
{
    public class SegToken
    {
        public int offset;
        public int length;
        public string strTag; //CRF对应于term组合后的Tag字符串
        public double fWeight;   //对应属性id的概率值，或者得分
    };

    public class crf_seg_out : CRFTermOut
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

        public crf_seg_out(int max_word_num = BaseUtils.DEFAULT_CRF_MAX_WORD_NUM):
            base(max_word_num)
        {
            termTotalLength = 0;
            tokenList = new List<SegToken>();
        }
    };

}
