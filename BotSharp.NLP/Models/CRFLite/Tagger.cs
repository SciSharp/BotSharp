using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BotSharp.Models.CRFLite
{
    public class Tagger
    {
        public List<List<string>> x_;
        public Node[,] node_; //Node matrix
        public short ysize_;
        public short word_num; //the number of tokens need to be labeled
        public double Z_;  //概率值
        public double cost_;  //The path cost
        public short[] result_;
        public List<long[]> feature_cache_;

        //Calculate the cost of each path. It's used for finding the best or N-best result
        public int viterbi()
        {
            var bestc = double.MinValue;
            Node bestNode = null;

            for (var i = 0; i < word_num; ++i)
            {
                for (var j = 0; j < ysize_; ++j)
                {
                    bestc = double.MinValue;
                    bestNode = null;

                    var node_i_j = node_[i, j];

                    for (int index = 0; index < node_i_j.lpathList.Count; ++index)
                    {
                        var p = node_i_j.lpathList[index];
                        var cost = p.lnode.bestCost + p.cost + node_i_j.cost;
                        if (cost > bestc)
                        {
                            bestc = cost;
                            bestNode = p.lnode;
                        }
                    }

                    node_i_j.prev = bestNode;
                    node_i_j.bestCost = bestNode != null ? bestc : node_i_j.cost;
                }
            }

            bestc = double.MinValue;
            bestNode = null;

            var s = (short)(word_num - 1);
            for (short j = 0; j < ysize_; ++j)
            {
                if (bestc < node_[s, j].bestCost)
                {
                    bestNode = node_[s, j];
                    bestc = node_[s, j].bestCost;
                }
            }

            var n = bestNode;
            while (n != null)
            {
                result_[n.x] = n.y;
                n = n.prev;
            }

            cost_ = -node_[s, result_[s]].bestCost;

            return BaseUtils.RETURN_SUCCESS;
        }

        private void calcAlpha(int m, int n)
        {
            var nd = node_[m, n];
            nd.alpha = 0.0;

            var i = 0;
            for (int index = 0; index < nd.lpathList.Count; index++)
            {
                var p = nd.lpathList[index];
                nd.alpha = BaseUtils.logsumexp(nd.alpha, p.cost + p.lnode.alpha, (i == 0));
                i++;
            }
            nd.alpha += nd.cost;
        }

        private void calcBeta(int m, int n)
        {
            var nd = node_[m, n];
            nd.beta = 0.0f;
            if (m + 1 < word_num)
            {
                var i = 0;
                for (int index = 0; index < nd.rpathList.Count; index++)
                {
                    var p = nd.rpathList[index];
                    nd.beta = BaseUtils.logsumexp(nd.beta, p.cost + p.rnode.beta, (i == 0));
                    i++;
                }
            }
            nd.beta += nd.cost;
        }

        public void forwardbackward()
        {
            for (int i = 0, k = word_num - 1; i < word_num; ++i, --k)
            {
                for (var j = 0; j < ysize_; ++j)
                {
                    calcAlpha(i, j);
                    calcBeta(k, j);
                }
            }

            Z_ = 0.0;
            for (var j = 0; j < ysize_; ++j)
            {
                Z_ = BaseUtils.logsumexp(Z_, node_[0, j].beta, j == 0);
            }
        }


        //Assign feature ids to node and path
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RebuildFeatures()
        {
            var fid = 0;
            for (short cur = 0; cur < word_num; ++cur)
            {
                for (short i = 0; i < ysize_; ++i)
                {
                    node_[cur, i].fid = fid;
                    if (cur > 0)
                    {
                        Node previousNode = node_[cur - 1, i];
                        for (int index = 0; index < previousNode.rpathList.Count; ++index)
                        {
                            Path path = previousNode.rpathList[index];
                            path.fid = fid + word_num - 1;
                        }
                    }
                }

                ++fid;
            }

            return 0;
        }
    }
}
