using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiebaNet.Segmenter
{
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                {
                    Add(key, default(TValue));
                }
                return base[key];
            }
            set { base[key] = value; }
        }
    }
}
