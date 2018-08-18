using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Decoder
{
    public class DecoderTagger : Tagger
    {
        private readonly Pool<StringBuilder> _buildersPool =
            new Pool<StringBuilder>(p => new StringBuilder(100), b => b.Clear());

        public int forward_backward_stat; //前向后向过程运行状态，0为未运行，1为已经运行

        //概率计算函数
        double toprob(Node n, double Z)
        {
            return Math.Exp(n.alpha + n.beta - n.cost - Z);
        }

        //To get the fastest decoded result, please set vlevel=0 and nbest=1, since it only outputs 1-best result without probability (forward-backward and A* aren't performed, only run viterbi)
        public int vlevel_; //Need to calculate probability 0 - no need to calculate, 1 - calculate sequence label probability, 2 - calculate both sequence label and individual entity probability
        protected int nbest_; //output top N-best result
        //CrfModel model;
        ModelReader featureIndex;

        Node node(int i, int j)
        {
            return node_[i, j];
        }

        Heap heap_queue; //Using min-heap to get next result, it's only used when nbest > 1
        public int crf_max_word_num;

        public DecoderTagger(int nbest, int this_crf_max_word_num = BaseUtils.DEFAULT_CRF_MAX_WORD_NUM)
        {
            crf_max_word_num = this_crf_max_word_num;
            vlevel_ = 0;
            nbest_ = nbest;
            cost_ = 0.0;
            Z_ = 0;

            ysize_ = 0;
            word_num = 0;
            heap_queue = null;
            node_ = null;
            x_ = null;
            result_ = null;
        }

        public void InitializeFeatureCache()
        {
            feature_cache_ = new List<long[]>();
            var feature_cache_every_row_size = 0;
            if (featureIndex.unigram_templs_.Count > featureIndex.bigram_templs_.Count)
            {
                feature_cache_every_row_size = featureIndex.unigram_templs_.Count + 1;
            }
            else
            {
                feature_cache_every_row_size = featureIndex.bigram_templs_.Count + 1;
            }
            for (var i = 0; i < crf_max_word_num * 2; i++)
            {
                var features = new long[feature_cache_every_row_size];
                for (var j = 0; j < feature_cache_every_row_size; j++)
                {
                    features[j] = -1;
                }
                feature_cache_.Add(features);
            }
        }

        //获取序列的词数
        public short get_word_num()
        {
            return word_num;
        }

        public double prob(int i, int j)
        {
            return toprob(node_[i, j], Z_);
        }

        //Get the probability of the i-th word's best result
        public double prob(int i)
        {
            return toprob(node_[i, result_[i]], Z_);
        }

        //Get entire sequence probability
        public double prob()
        {
            return Math.Exp(-cost_ - Z_);
        }

        //Get the string of i-th tag
        public string yname(int i) { return featureIndex.y(i); }

        //设置vlevel
        public void set_vlevel(int vlevel_value)
        {
            vlevel_ = vlevel_value;
        }

        //使用模型初始化tag，必须先使用该函数初始化才能使用add和parse                                                                    
        //正常返回为0， 错误返回<0
        public int init_by_model(ModelReader model_p)
        {
            featureIndex = model_p;
            ysize_ = (short)model_p.ysize();

            if (nbest_ > 1)
            {
                //Only allocate heap when nbest is more than 1
                heap_queue = BaseUtils.heap_init((int)(crf_max_word_num * ysize_ * ysize_));
            }

            //Initialize feature set cache according unigram and bigram templates
            InitializeFeatureCache();

            node_ = new Node[crf_max_word_num, ysize_];
            result_ = new short[crf_max_word_num];

            //Create node and path cache
            for (short cur = 0; cur < crf_max_word_num; cur++)
            {
                for (short i = 0; i < ysize_; i++)
                {
                    var n = new Node();
                    node_[cur, i] = n;

                    n.lpathList = new List<Path>();
                    n.rpathList = new List<Path>();
                    n.x = cur;
                    n.y = i;
                }
            }

            for (var cur = 1; cur < crf_max_word_num; cur++)
            {
                for (var j = 0; j < ysize_; ++j)
                {
                    for (var i = 0; i < ysize_; ++i)
                    {
                        var p = new Path();
                        p.add(node_[cur - 1, j], node_[cur, i]);
                    }
                }
            }

            return BaseUtils.ERROR_SUCCESS;
        }

        public int initNbest()
        {
            var k = (int)word_num - 1;
            for (var i = 0; i < ysize_; ++i)
            {
                var eos = BaseUtils.allc_from_heap(heap_queue);
                eos.node = node_[k, i];
                eos.fx = -node_[k, i].bestCost;
                eos.gx = -node_[k, i].cost;
                eos.next = null;
                if (BaseUtils.heap_insert(eos, heap_queue) < 0)
                {
                    return BaseUtils.ERROR_INSERT_HEAP_FAILED;
                }
            }
            return BaseUtils.ERROR_SUCCESS;
        }

        public int next()
        {
            while (!BaseUtils.is_heap_empty(heap_queue))
            {
                var top = BaseUtils.heap_delete_min(heap_queue);
                var rnode = top.node;

                if (rnode.x == 0)
                {
                    for (var n = top; n != null; n = n.next)
                    {
                        result_[n.node.x] = n.node.y;
                    }
                    cost_ = top.gx;
                    return 0;
                }

                for (int index = 0; index < rnode.lpathList.Count; index++)
                {
                    var p = rnode.lpathList[index];
                    var n = BaseUtils.allc_from_heap(heap_queue);
                    var x_num = (rnode.x) - 1;
                    n.node = p.lnode;
                    n.gx = -p.lnode.cost - p.cost + top.gx;
                    n.fx = -p.lnode.bestCost - p.cost + top.gx;
                    //          |              h(x)                 |  |  g(x)  |
                    n.next = top;
                    if (BaseUtils.heap_insert(n, heap_queue) < 0)
                    {
                        return BaseUtils.ERROR_INSERT_HEAP_FAILED;
                    }
                }
            }
            return 0;
        }

        public int reset()
        {
            word_num = 0;
            Z_ = cost_ = 0.0;

            BaseUtils.heap_reset(heap_queue);
            return BaseUtils.ERROR_SUCCESS;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int buildLattice()
        {
            //Generate feature ids for all nodes and paths
            RebuildFeatures();

            for (int i = 0; i < word_num; ++i)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    var currentNode = node_[i, j];
                    calcCost(currentNode);
                    for (int index = 0; index < currentNode.lpathList.Count; ++index)
                    {
                        var p = currentNode.lpathList[index];
                        calcCost(p);
                    }
                }
            }

            return BaseUtils.ERROR_SUCCESS;
        }

        public int add(List<List<string>> row_p)
        {
            x_ = row_p;
            word_num = (short)x_.Count;

            return BaseUtils.ERROR_SUCCESS;
        }


        public int termbuf_build(CRFTermOut term_buf)
        {
            if (vlevel_ > 0)
            {
                //Calcuate the sequence label probability
                term_buf.prob = prob();
            }

            var this_word_num = get_word_num();

            for (var i = 0; i < this_word_num; ++i)
            {
                term_buf.result_[i] = yname(result_[i]);
                switch (vlevel_)
                {
                    case 0:
                        term_buf.weight_[i] = 0.0;
                        break;
                    case 2:
                        term_buf.weight_[i] = prob(i);
                        break;
                }
            }
            return BaseUtils.ERROR_SUCCESS;
        }

        //Label input string. The result is saved as result []
        //If nbest > 1, get nbest result by "next"
        //Returen value: Successed - 0, Failed < 0
        public int parse()
        {
            var ret = 0;
            //no word need to be labeled
            if (word_num == 0)
            {
                return BaseUtils.ERROR_SUCCESS;
            }

            //building feature set
            ret = buildFeatures();
            if (ret < 0)
            {
                return ret;
            }


            ret = buildLattice();
            if (ret < 0)
            {
                return ret;
            }

            //4.forward-backward when we need to calcuate probability
            if (vlevel_ > 0)
            {
                forwardbackward();
            }


            //5.using viterbi to search best result path
            ret = viterbi();
            if (ret < 0)
            {
                return ret;
            }

            //6.initNbest
            //	求nbest(n>1)时的数据结构初始化，此后可以调用next()来获取nbest结果
            if (nbest_ > 1)
            {
                //如果只求1-best，不需要使用initNbest()和next()获取结果
                ret = initNbest();
                if (ret < 0)
                {
                    return ret;
                }

            }

            return BaseUtils.ERROR_SUCCESS;
        }


        public int buildFeatures()
        {
            if (word_num <= 0)
            {
                return BaseUtils.ERROR_INVALIDATED_PARAMETER;
            }
            using (var v = _buildersPool.GetOrCreate())
            {
                var builder = v.Item;
                var id = 0;
                var feature_cache_row_size = 0;
                var feature_cache_size = 0;
                for (var cur = 0; cur < word_num; cur++)
                {
                    feature_cache_row_size = 0;
                    for (int index = 0; index < featureIndex.unigram_templs_.Count; index++)
                    {
                        var templ = featureIndex.unigram_templs_[index];
                        var res = featureIndex.apply_rule(templ, cur, builder, this);
                        if (res == null)
                        {
                            return BaseUtils.ERROR_EMPTY_FEATURE;
                        }
                        id = featureIndex.get_id(res.ToString());
                        if (id != -1)
                        {
                            feature_cache_[feature_cache_size][feature_cache_row_size] = id;
                            feature_cache_row_size++;
                        }
                    }
                    feature_cache_[feature_cache_size][feature_cache_row_size] = -1;
                    feature_cache_size++;
                }

                for (var cur = 0; cur < word_num; cur++)
                {
                    feature_cache_row_size = 0;
                    for (int index = 0; index < featureIndex.bigram_templs_.Count; index++)
                    {
                        var templ = featureIndex.bigram_templs_[index];
                        var strFeature = featureIndex.apply_rule(templ, cur, builder, this);
                        if (strFeature == null)
                        {
                            return BaseUtils.ERROR_EMPTY_FEATURE;
                        }

                        id = featureIndex.get_id(strFeature.ToString());
                        if (id != -1)
                        {
                            feature_cache_[feature_cache_size][feature_cache_row_size] = id;
                            feature_cache_row_size++;
                        }
                    }
                    feature_cache_[feature_cache_size][feature_cache_row_size] = -1;
                    feature_cache_size++;
                }

                return BaseUtils.ERROR_SUCCESS;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void calcCost(Node n)
        {
            double c = 0;
            var f = feature_cache_[n.fid];

            for (int i = 0; i < f.Length; ++i)
            {
                int fCurrent = (int)f[i];
                if (fCurrent == -1)
                    break;
                c += featureIndex.GetAlpha(fCurrent + n.y);
            }

            n.cost = featureIndex.cost_factor_ * c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void calcCost(Path p)
        {
            double c = 0;
            long[] f = feature_cache_[p.fid];
            for (int i = 0; i < f.Length; ++i)
            {
                int fCurrent = (int)f[i];
                if (fCurrent == -1)
                    break;
                c += featureIndex.GetAlpha((fCurrent + p.lnode.y * ysize_ + p.rnode.y));
            }

            p.cost = featureIndex.cost_factor_ * c;
        }


        public int output(CRFTermOut[] pout)
        {
            var n = 0;
            var ret = 0;

            if (nbest_ == 1)
            {
                //If only best result and no need probability, "next" is not to be used
                ret = termbuf_build(pout[0]);
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

                    ret = termbuf_build(pout[n]);
                    if (ret < 0)
                    {
                        return ret;
                    }
                }
            }

            return BaseUtils.ERROR_SUCCESS;
        }
    }
}
