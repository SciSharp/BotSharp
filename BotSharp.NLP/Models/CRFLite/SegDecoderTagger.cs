using BotSharp.Models.CRFLite.Decoder;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Models.CRFLite
{
    public class SegDecoderTagger : DecoderTagger
    {
        public SegDecoderTagger(int nbest, int this_crf_max_word_num = BaseUtils.DEFAULT_CRF_MAX_WORD_NUM)
            : base(nbest, this_crf_max_word_num)
        {
            crf_max_word_num = this_crf_max_word_num;
        }

        int seg_termbuf_build(crf_seg_out term_buf)
        {
            term_buf.Clear();

            //build raw result at first
            var iRet = termbuf_build(term_buf);
            if (iRet != BaseUtils.RETURN_SUCCESS)
            {
                return iRet;
            }

            //Then build token result
            var term_len = 0;
            var weight = 0.0;
            var num = 0;
            for (var i = 0; i < x_.Count; i++)
            {
                //Adding the length of current token
                var strTag = term_buf.result_[i];
                term_len += x_[i][0].Length;
                weight += term_buf.weight_[i];
                num++;

                //Check if current term is the end of a token
                if ((strTag.StartsWith("B_") == false &&
                    strTag.StartsWith("M_") == false) ||
                    i == x_.Count - 1)
                {
                    var tkn = new SegToken();
                    tkn.length = term_len;
                    tkn.offset = term_buf.termTotalLength;

                    var spos = strTag.IndexOf('_');
                    if (spos < 0)
                    {
                        if (strTag == "NOR")
                        {
                            tkn.strTag = "";
                        }
                        else
                        {
                            tkn.strTag = strTag;
                        }
                    }
                    else
                    {
                        tkn.strTag = strTag.Substring(spos + 1);
                    }

                    term_buf.termTotalLength += term_len;
                    //Calculate each token's weight
                    switch (vlevel_)
                    {
                        case 0:
                            tkn.fWeight = 0.0;
                            break;
                        case 2:
                            tkn.fWeight = weight / num;
                            weight = 0.0;
                            num = 0;
                            break;
                    }

                    term_buf.tokenList.Add(tkn);
                    term_len = 0;
                }
            }


            return BaseUtils.RETURN_SUCCESS;
        }


        public int output(crf_seg_out[] pout)
        {
            var n = 0;
            var ret = 0;

            if (nbest_ == 1)
            {
                //If only best result and no need probability, "next" is not to be used
                ret = seg_termbuf_build(pout[0]);
                if (ret < 0)
                {
                    return ret;
                }
            }
            else
            {
                //Fill the n best result
                var iNBest = nbest_;
                if (pout.Length < iNBest)
                {
                    iNBest = pout.Length;
                }

                for (n = 0; n < iNBest; ++n)
                {
                    ret = next();
                    if (ret < 0)
                    {
                        break;
                    }

                    ret = seg_termbuf_build(pout[n]);
                    if (ret < 0)
                    {
                        return ret;
                    }
                }
            }

            return BaseUtils.RETURN_SUCCESS;
        }
    }
}
