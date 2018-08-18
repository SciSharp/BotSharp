using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Decoder
{
    public class CRFTermOut
    {
        //Sequence label probability
        public double prob;

        //Raw CRF model output
        public string[] result_;
        public double[] weight_;

        public CRFTermOut(int max_word_num = BaseUtils.DEFAULT_CRF_MAX_WORD_NUM)
        {
            prob = 0;
            result_ = new string[max_word_num];
            weight_ = new double[max_word_num];
        }
    }
}
