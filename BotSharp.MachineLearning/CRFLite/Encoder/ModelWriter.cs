using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BotSharp.MachineLearning.CRFLite.Decoder;

namespace BotSharp.MachineLearning.CRFLite.Encoder
{
    public class ModelWriter : BaseModel
    {
        private readonly string modelFileName;

        private readonly Pool<StringBuilder> _buildersPool =
            new Pool<StringBuilder>(p => new StringBuilder(100), b => b.Clear());


        int thread_num_;
        public IFeatureLexicalDict featureLexicalDict;
        List<List<List<string>>> trainCorpusList;
        ParallelOptions parallelOption = new ParallelOptions();

        public ModelWriter(int thread_num, double cost_factor,
            uint hugeLexShrinkMemLoad, string modelFileName)
        {
            cost_factor_ = cost_factor;
            maxid_ = 0;
            thread_num_ = thread_num;
            this.modelFileName = modelFileName;
            parallelOption.MaxDegreeOfParallelism = thread_num;

            if (hugeLexShrinkMemLoad > 0)
            {
                featureLexicalDict = new HugeFeatureLexicalDict(thread_num_, hugeLexShrinkMemLoad);
            }
            else
            {
                featureLexicalDict = new DefaultFeatureLexicalDict(thread_num_);
            }
        }

        //Regenerate feature id and shrink features with lower frequency
        public void Shrink(EncoderTagger[] xList, int freq)
        {
            var old2new = new CRFLite.Utils.BTreeDictionary<long, long>();
            featureLexicalDict.Shrink(freq);
            maxid_ = featureLexicalDict.RegenerateFeatureId(old2new, y_.Count);
            var feature_count = xList.Length;

            //Update feature ids
            Parallel.For(0, feature_count, parallelOption, i =>
            {
                for (var j = 0; j < xList[i].feature_cache_.Count; j++)
                {
                    var newfs = new List<long>();
                    long rstValue = 0;
                    for (int index = 0; index < xList[i].feature_cache_[j].Length; index++)
                    {
                        var v = xList[i].feature_cache_[j][index];
                        if (old2new.TryGetValue(v, out rstValue) == true)
                        {
                            newfs.Add(rstValue);
                        }
                    }
                    xList[i].feature_cache_[j] = newfs.ToArray();
                }
            });
        }

        // Load all records and generate features
        public EncoderTagger[] ReadAllRecords()
        {
            var arrayEncoderTagger = new EncoderTagger[trainCorpusList.Count];
            var arrayEncoderTaggerSize = 0;

            //Generate each record features
            Parallel.For(0, trainCorpusList.Count, parallelOption, i =>
            {
                var _x = new EncoderTagger(this);
                if (_x.GenerateFeature(trainCorpusList[i]) == false)
                {
                }
                else
                {
                    var oldValue = Interlocked.Increment(ref arrayEncoderTaggerSize) - 1;
                    arrayEncoderTagger[oldValue] = _x;

                    if (oldValue % 10000 == 0)
                    {
                        //Show current progress on console
                        Console.Write("{0}...", oldValue);
                    }
                }
            });

            trainCorpusList.Clear();
            trainCorpusList = null;

            Console.WriteLine();
            return arrayEncoderTagger;
        }

        //Open and check training and template file
        public bool Open(string strTemplateFileName, string strTrainCorpusFileName)
        {
            return OpenTemplateFile(strTemplateFileName) && OpenTrainCorpusFile(strTrainCorpusFileName);
        }

        //Build feature set into indexed data
        public bool BuildFeatureSetIntoIndex(string filename, double max_slot_usage_rate_threshold, int debugLevel)
        {
            IList<string> keyList;
            IList<int> valList;
            featureLexicalDict.GenerateLexicalIdList(out keyList, out valList);

            if (debugLevel > 0)
            {
                var filename_featureset_raw_format = filename + ".feature.raw_text";
                var sw = new StreamWriter(filename_featureset_raw_format);
                // save feature and its id into lists in raw format
                for (var i = 0; i < keyList.Count; i++)
                {
                    sw.WriteLine("{0}\t{1}", keyList[i], valList[i]);
                }
                sw.Close();
            }

            //Build feature index
            var filename_featureset = filename + ".feature";
            var da = new CRFLite.Utils.DoubleArrayTrieBuilder(thread_num_);
            if (da.build(keyList, valList, max_slot_usage_rate_threshold) == false)
            {
                return false;
            }
            //Save indexed feature set into file
            da.save(filename_featureset);

            if (string.IsNullOrWhiteSpace(modelFileName))
            {
                //Clean up all data
                featureLexicalDict.Clear();
                featureLexicalDict = null;
                keyList = null;
                valList = null;

                GC.Collect();

                //Create weight matrix
                alpha_ = new double[feature_size() + 1];
            }
            else
            {
                //Create weight matrix
                alpha_ = new double[feature_size() + 1];
                var modelReader = new ModelReader(this.modelFileName);
                modelReader.LoadModel();

                if (modelReader.y_.Count == y_.Count)
                {
                    for (var i = 0; i < keyList.Count; i++)
                    {
                        var index = modelReader.get_id(keyList[i]);
                        if (index < 0)
                        {
                            continue;
                        }
                        var size = (keyList[i][0] == 'U' ? y_.Count : y_.Count * y_.Count);
                        for (var j = 0; j < size; j++)
                        {
                            alpha_[valList[i] + j + 1] = modelReader.GetAlpha(index + j);
                        }
                    }
                }
                else
                {
                }

                //Clean up all data
                featureLexicalDict.Clear();
                featureLexicalDict = null;
                keyList = null;
                valList = null;

                GC.Collect();
            }

            return true;
        }

        //Save model meta data into file
        public bool SaveModelMetaData(string filename)
        {
            var tofs = new StreamWriter(filename);

            // header
            tofs.WriteLine("version: " + BaseUtils.MODEL_TYPE_NORM);
            tofs.WriteLine("cost-factor: " + cost_factor_);
            tofs.WriteLine("maxid: " + maxid_);
            tofs.WriteLine("xsize: " + xsize_);

            tofs.WriteLine();

            // y
            for (var i = 0; i < y_.Count; ++i)
            {
                tofs.WriteLine(y_[i]);
            }
            tofs.WriteLine();

            // template
            for (var i = 0; i < unigram_templs_.Count; ++i)
            {
                tofs.WriteLine(unigram_templs_[i]);
            }
            for (var i = 0; i < bigram_templs_.Count; ++i)
            {
                tofs.WriteLine(bigram_templs_[i]);
            }

            tofs.Close();

            return true;
        }

        /// <summary>
        /// Save feature weights into file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="bVQ"></param>
        /// <returns></returns>
        public void SaveFeatureWeight(string filename, bool bVQ)
        {
            var filename_alpha = filename + ".alpha";
            var tofs = new StreamWriter(filename_alpha, false);
            var bw = new BinaryWriter(tofs.BaseStream);

            if (bVQ == true)
            {
                //Build code book
                CRFLite.Utils.VectorQuantization vq = new CRFLite.Utils.VectorQuantization();
                for (long i = 1; i <= maxid_; i++)
                {
                    vq.Add(alpha_[i]);
                }

                int vqSize = 256;
                double distortion = vq.BuildCodebook(vqSize);

                //VQ size
                bw.Write(vqSize);

                //Save VQ codebook into file
                for (int j = 0; j < vqSize; j++)
                {
                    bw.Write(vq.CodeBook[j]);
                }

                //Save weights
                for (long i = 1; i <= maxid_; ++i)
                {
                    bw.Write((byte)vq.ComputeVQ(alpha_[i]));
                }
            }
            else
            {
                bw.Write(0);
                //Save weights
                for (long i = 1; i <= maxid_; ++i)
                {
                    bw.Write((float)alpha_[i]);
                }
            }

            bw.Close();
        }

        bool OpenTemplateFile(string filename)
        {
            var ifs = new StreamReader(filename);
            unigram_templs_ = new List<string>();
            bigram_templs_ = new List<string>();
            while (ifs.EndOfStream == false)
            {
                var line = ifs.ReadLine();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                if (line[0] == 'U')
                {
                    unigram_templs_.Add(line);
                }
                else if (line[0] == 'B')
                {
                    bigram_templs_.Add(line);
                }
                else
                {
                }
            }
            ifs.Close();
            return true;
        }

        bool OpenTrainCorpusFile(string strTrainingCorpusFileName)
        {
            var ifs = new StreamReader(strTrainingCorpusFileName);
            y_ = new List<string>();
            trainCorpusList = new List<List<List<string>>>();
            var hashCand = new HashSet<string>();
            var recordList = new List<List<string>>();

            var last_xsize = -1;
            while (ifs.EndOfStream == false)
            {
                var line = ifs.ReadLine();
                if (line.Length == 0 || line[0] == ' ' || line[0] == '\t')
                {
                    //Current record is finished, save it into the list
                    if (recordList.Count > 0)
                    {
                        trainCorpusList.Add(recordList);
                        recordList = new List<List<string>>();
                    }
                    continue;
                }

                var items = line.Split('\t');
                var size = items.Length;
                if (last_xsize >= 0 && last_xsize != size)
                {
                    return false;
                }
                last_xsize = size;
                xsize_ = (uint)(size - 1);
                recordList.Add(new List<string>(items));

                if (hashCand.Contains(items[items.Length - 1]) == false)
                {
                    hashCand.Add(items[items.Length - 1]);
                    y_.Add(items[items.Length - 1]);
                }
            }
            ifs.Close();

            return true;
        }

        //Get feature id from feature set by feature string
        //If feature string is not existed in the set, generate a new id and return it
        public bool BuildFeatures(EncoderTagger tagger)
        {
            var feature = new List<long>();
            using (var v = _buildersPool.GetOrCreate())
            {
                var localBuilder = v.Item;
                //tagger.feature_id_ = tagger.feature_cache_.Count;
                for (var cur = 0; cur < tagger.word_num; ++cur)
                {
                    for (int index = 0; index < unigram_templs_.Count; index++)
                    {
                        var it = unigram_templs_[index];
                        var strFeature = apply_rule(it, cur, localBuilder, tagger);
                        if (strFeature == null)
                        {
                        }
                        else
                        {
                            var id = featureLexicalDict.GetOrAddId(strFeature.ToString());
                            feature.Add(id);
                        }
                    }
                    tagger.feature_cache_.Add(feature.ToArray());
                    feature.Clear();
                }

                for (var cur = 1; cur < tagger.word_num; ++cur)
                {
                    for (int index = 0; index < bigram_templs_.Count; index++)
                    {
                        var it = bigram_templs_[index];
                        var strFeature = apply_rule(it, cur, localBuilder, tagger);
                        if (strFeature == null)
                        {
                        }
                        else
                        {
                            var id = featureLexicalDict.GetOrAddId(strFeature.ToString());
                            feature.Add(id);
                        }
                    }

                    tagger.feature_cache_.Add(feature.ToArray());
                    feature.Clear();

                }

            }

            return true;
        }

    }
}
