using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Encoder
{
    public interface IFeatureLexicalDict
    {
        void Shrink(int freq);
        long GetOrAddId(string strFeature);
        long RegenerateFeatureId(CRFLite.Utils.BTreeDictionary<long, long> old2new, long ysize);
        void GenerateLexicalIdList(out IList<string> fea, out IList<int> val);
        void Clear();

        long Size
        {
            get;
        }
    }
}
