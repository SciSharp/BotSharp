using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace BotSharp.MachineLearning.CRFLite.Encoder
{
    public class EncoderTagger : Tagger
    {
        public ModelWriter feature_index_;
        public short[] answer_;

        public int eval(int[,] merr)
        {
            var err = 0;
            for (var i = 0; i < word_num; ++i)
            {
                if (answer_[i] != result_[i])
                {
                    ++err;
                    merr[answer_[i], result_[i]]++;
                }
            }
            return err;
        }

        public EncoderTagger(ModelWriter modelWriter)
        {
            feature_index_ = modelWriter;
            ysize_ = (short)feature_index_.ysize();
        }

        public bool GenerateFeature(List<List<string>> recordList)
        {
            word_num = (short)recordList.Count;
            if (word_num == 0)
            {
                return false;
            }

            //Try to find each record's answer tag
            var x_num = 0;
            var xsize = (int)feature_index_.xsize_;
            answer_ = new short[word_num];
            for (int index = 0; index < recordList.Count; index++)
            {
                var record = recordList[index];
//get result tag's index and fill answer
                for (short k = 0; k < ysize_; ++k)
                {
                    if (feature_index_.y(k) == record[xsize])
                    {
                        answer_[x_num] = k;
                        break;
                    }
                }
                x_num++;
            }

            //Build record feature set
            x_ = recordList;
            Z_ = 0.0;
            feature_cache_ = new List<long[]>();
            feature_index_.BuildFeatures(this);
            x_ = null;

            return true;
        }

        private void LockFreeAdd(double[] expected, long exp_offset, double addValue)
        {
            double initialValue;
            double newValue;
            do
            {
                initialValue = expected[exp_offset]; // read current value
                newValue = initialValue + addValue;  //calculate new value
            }
            while (initialValue != Interlocked.CompareExchange(ref expected[exp_offset], newValue, initialValue));
        }

        private void calcExpectation(int x, int y, double[] expected)
        {
            var n = node_[x, y];
            var c = Math.Exp(n.alpha + n.beta - n.cost - Z_);
            var offset = y + 1; //since expected array is based on 1
            for (int index = 0; index < feature_cache_[n.fid].Length; index++)
            {
                var item = feature_cache_[n.fid][index];
                LockFreeAdd(expected, item + offset, c);
            }

            for (int index = 0; index < n.lpathList.Count; index++)
            {
                var p = n.lpathList[index];
                c = Math.Exp(p.lnode.alpha + p.cost + p.rnode.beta - Z_);
                offset = p.lnode.y * ysize_ + p.rnode.y + 1; //since expected array is based on 1
                for (int i = 0; i < feature_cache_[p.fid].Length; i++)
                {
                    var item = feature_cache_[p.fid][i];
                    LockFreeAdd(expected, item + offset, c);
                }
            }
        }

        public double gradient(double[] expected)
        {
            buildLattice();
            forwardbackward();
            var s = 0.0;

            for (var i = 0; i < word_num; ++i)
            {
                for (var j = 0; j < ysize_; ++j)
                {
                    calcExpectation(i, j, expected);
                }
            }

            for (var i = 0; i < word_num; ++i)
            {
                var answer_val = answer_[i];
                var answer_Node = node_[i, answer_val];
                var offset = answer_val + 1; //since expected array is based on 1
                for (int index = 0; index < feature_cache_[answer_Node.fid].Length; index++)
                {
                    var fid = feature_cache_[answer_Node.fid][index];
                    LockFreeAdd(expected, fid + offset, -1.0f);
                }
                s += answer_Node.cost;  // UNIGRAM cost


                for (int index = 0; index < answer_Node.lpathList.Count; index++)
                {
                    var lpath = answer_Node.lpathList[index];
                    if (lpath.lnode.y == answer_[lpath.lnode.x])
                    {
                        offset = lpath.lnode.y * ysize_ + lpath.rnode.y + 1;
                        for (int index1 = 0; index1 < feature_cache_[lpath.fid].Length; index1++)
                        {
                            var fid = feature_cache_[lpath.fid][index1];
                            LockFreeAdd(expected, fid + offset, -1.0f);
                        }

                        s += lpath.cost; // BIGRAM COST
                        break;
                    }
                }
            }

            viterbi();  // call for eval()
            return Z_ - s;
        }

        public void Init(short[] result, Node[,] node)
        {
            result_ = result;
            node_ = node;
        }




        public void buildLattice()
        {
            RebuildFeatures();
            for (var i = 0; i < word_num; ++i)
            {
                for (var j = 0; j < ysize_; ++j)
                {
                    var node_i_j = node_[i, j];
                    node_i_j.cost = calcCost(node_i_j.fid, j);
                    for (int index = 0; index < node_i_j.lpathList.Count; index++)
                    {
                        var p = node_i_j.lpathList[index];
                        var offset = p.lnode.y * ysize_ + p.rnode.y;
                        p.cost = calcCost(p.fid, offset);
                    }
                }
            }
        }

        public double calcCost(int featureListIdx, int offset)
        {
            double c = 0.0f;
            offset++; //since alpha_ array is based on 1
            for (int index = 0; index < feature_cache_[featureListIdx].Length; index++)
            {
                var fid = feature_cache_[featureListIdx][index];
                c += feature_index_.alpha_[fid + offset];
            }
            return feature_index_.cost_factor_ * c;
        }
    }
}
