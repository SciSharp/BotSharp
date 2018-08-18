using System.Collections.Generic;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite
{
    public class BaseModel
    {
        public long maxid_;
        public double cost_factor_;

        public List<string> unigram_templs_;
        public List<string> bigram_templs_;

        //Labeling tag list
        public List<string> y_;
        public uint ysize() { return (uint)y_.Count; }

        //The dimension training corpus
        public uint xsize_;

        //Feature set value array
        public double[] alpha_;

        public BaseModel()
        {
            cost_factor_ = 1.0;
        }

        //获取类别i的字符表示
        public string y(int i) { return y_[i]; }

        public long feature_size() { return maxid_; }

        public StringBuilder apply_rule(string p, int pos, StringBuilder resultContainer, Tagger tagger)
        {
            resultContainer.Clear();
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] == '%')
                {
                    i++;
                    if (p[i] == 'x')
                    {
                        i++;
                        var res = get_index(p, pos, i, tagger);
                        i = res.idx;
                        if (res.value == null)
                        {
                            return null;
                        }
                        resultContainer.Append(res.value);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    resultContainer.Append(p[i]);
                }
            }
            return resultContainer;
        }

        Index get_index(string p, int pos, int i, Tagger tagger)
        {
            if (p[i] != '[')
            {
                return new Index(null, i);
            }
            i++;
            var isInRow = true;
            var col = 0;
            var row = 0;
            var neg = 1;

            if (p[i] == '-')
            {
                neg = -1;
                i++;
            }

            for (; i < p.Length; i++)
            {
                var c = p[i];
                if (isInRow)
                {
                    if (c >= '0' && c <= '9')
                    {
                        row = 10 * row + (c - '0');
                    }
                    else if (c == ',')
                    {
                        isInRow = false;
                    }
                    else
                    {
                        return new Index(null, i);
                    }
                }
                else
                {
                    if (c >= '0' && c <= '9')
                    {
                        col = 10 * col + (c - '0');
                    }
                    else if (c == ']')
                    {
                        break;
                    }
                    else
                    {
                        return new Index(null, i);
                    }
                }
            }

            row *= neg;

            if (col < 0 || col >= xsize_)
            {
                return new Index(null, i);
            }
            var idx = pos + row;
            if (idx < 0)
            {
                return new Index("_B-" + (-idx).ToString(), i); ;
            }
            if (idx >= tagger.word_num)
            {
                return new Index("_B+" + (idx - tagger.word_num + 1).ToString(), i);
            }

            return new Index(tagger.x_[idx][col], i);

        }

        private struct Index
        {
            public int idx;
            public string value;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:System.Object"/> class.
            /// </summary>
            public Index(string value, int idx)
            {
                this.idx = idx;
                this.value = value;
            }
        }
    }
}
