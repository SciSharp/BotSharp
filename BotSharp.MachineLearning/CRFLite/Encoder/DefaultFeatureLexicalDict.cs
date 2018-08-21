using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.MachineLearning.CRFLite.Encoder
{
    public class DefaultFeatureLexicalDict : IFeatureLexicalDict
    {
        CRFLite.Utils.BTreeDictionary<string, FeatureIdPair> featureset_dict_;
        long maxid_;
        Object thisLock = new object();
        ParallelOptions parallelOption;

        public DefaultFeatureLexicalDict(int thread_num)
        {
            featureset_dict_ = new CRFLite.Utils.BTreeDictionary<string, FeatureIdPair>(StringComparer.Ordinal, 128);
            maxid_ = 0;
            parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = thread_num;
        }

        public void Clear()
        {
            featureset_dict_.Clear();
            featureset_dict_ = null;
        }

        public long Size
        {
            get
            {
                return featureset_dict_.Count;
            }
        }

        public void Shrink(int freq)
        {
            var i = 0;
            while (i < featureset_dict_.Count)
            {
                if (featureset_dict_.ValueList[i].Value < freq)
                {
                    //If the feature's frequency is less than specific frequency, drop the feature.
                    featureset_dict_.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void GenerateLexicalIdList(out IList<string> keyList, out IList<int> valList)
        {
            keyList = featureset_dict_.KeyList;
            var fixArrayValue = new int[Size];
            valList = fixArrayValue;

            Parallel.For(0, featureset_dict_.ValueList.Count, parallelOption, i =>
            {
                fixArrayValue[i] = (int)featureset_dict_.ValueList[i].Key;
            });

        }

        public long RegenerateFeatureId(CRFLite.Utils.BTreeDictionary<long, long> old2new, long ysize)
        {
            long new_maxid = 0;
            //Regenerate new feature id and create feature ids mapping
            foreach (var it in featureset_dict_)
            {
                var strFeature = it.Key;
                //Regenerate new feature id
                old2new.Add(it.Value.Key, new_maxid);
                it.Value.Key = new_maxid;

                var addValue = (strFeature[0] == 'U' ? ysize : ysize * ysize);
                new_maxid += addValue;
            }

            return new_maxid;
        }

        //Get feature id from feature set by feature string
        //If feature string is not existed in the set, generate a new id and return it
        private long GetId(string key)
        {
            FeatureIdPair pair;
            if (featureset_dict_.TryGetValue(key, out pair) == true)
            {
                return pair.Key;
            }

            return BaseUtils.RETURN_INVALIDATED_FEATURE;
        }

        public long GetOrAddId(string key)
        {
            FeatureIdPair pair;
            if (featureset_dict_.TryGetValue(key, out pair) == true && pair != null)
            {
                //Find its feature id
                System.Threading.Interlocked.Increment(ref pair.Value);
            }
            else
            {
                lock (thisLock)
                {
                    if (featureset_dict_.TryGetValue(key, out pair) == true)
                    {
                        System.Threading.Interlocked.Increment(ref pair.Value);
                    }
                    else
                    {
                        var oldValue = Interlocked.Increment(ref maxid_) - 1;
                        pair = new FeatureIdPair(oldValue, 1);
                        featureset_dict_.Add(key, pair);
                    }
                }
            }
            return pair.Key;
        }
    }
}
