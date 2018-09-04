using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BotSharp.Models.CRFLite.Encoder
{
    public class CRFEncoderThread
    {
        public EncoderTagger[] x;
        public int start_i;
        public int thread_num;
        public int zeroone;
        public int err;
        public double obj;
        public Node[,] node_;
        short[] result_;
        public short max_xsize_;
        public LBFGS lbfgs;
        public int[,] merr;

        public void Init()
        {
            if (x.Length == 0)
            {
                return;
            }

            var ysize_ = x[0].ysize_;
            max_xsize_ = 0;
            for (var i = start_i; i < x.Length; i += thread_num)
            {
                if (max_xsize_ < x[i].word_num)
                {
                    max_xsize_ = x[i].word_num;
                }
            }

            result_ = new short[max_xsize_];
            node_ = new Node[max_xsize_, ysize_];
            for (var i = 0; i < max_xsize_; i++)
            {
                for (var j = 0; j < ysize_; j++)
                {
                    node_[i, j] = new Node();
                    node_[i, j].x = (short)i;
                    node_[i, j].y = (short)j;
                    node_[i, j].lpathList = new List<Path>(ysize_);
                    node_[i, j].rpathList = new List<Path>(ysize_);
                }
            }

            for (short cur = 1; cur < max_xsize_; ++cur)
            {
                for (short j = 0; j < ysize_; ++j)
                {
                    for (short i = 0; i < ysize_; ++i)
                    {
                        var path = new Path();
                        path.fid = -1;
                        path.cost = 0.0;
                        path.add(node_[cur - 1, j], node_[cur, i]);
                    }
                }
            }

            merr = new int[ysize_, ysize_];
        }

        public void Run()
        {
            //Initialize thread self data structure
            obj = 0.0f;
            err = zeroone = 0;
            //expected.Clear();
            Array.Clear(merr, 0, merr.Length);
            for (var i = start_i; i < x.Length; i += thread_num)
            {
                x[i].Init(result_, node_);
                obj += x[i].gradient(lbfgs.expected);
                var error_num = x[i].eval(merr);
                err += error_num;
                if (error_num > 0)
                {
                    ++zeroone;
                }
            }
        }

    }
}
