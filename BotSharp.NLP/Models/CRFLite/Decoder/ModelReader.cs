using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BotSharp.Models.CRFLite.Utils;

namespace BotSharp.Models.CRFLite.Decoder
{
    public class ModelReader : BaseModel
    {
        private readonly Func<string, Stream> modelLoader = null;
             
        public uint version; //模型版本号,读取模型时读入
        private CRFLite.Utils.DoubleArrayTrieSearch da; //特征集合

        /// <summary>
        /// Returns the model path.
        /// </summary>
        public string ModelPath { get; private set; }

        /// <summary>
        /// Creates a new <see cref="ModelReader"/>
        /// that will load the model from the file system,
        /// using the given <paramref name="modelPath"/>.
        /// </summary>
        /// <param name="modelPath">
        /// Path to the model.
        /// </param>
        public ModelReader(string modelPath) :
            this(GetStreamFromFileSystem, modelPath)
        {
            
        }

        /// <summary>
        /// Creates a new <see cref="ModelReader"/>
        /// that will load the model from the file system,
        /// using the given <paramref name="modelPath"/>.
        /// </summary>
        /// <param name="modelLoader">
        /// A delegate capable of resolving 
        /// the given <paramref name="modelPath"/>
        /// into a stream with the model file.
        /// </param>
        /// <param name="modelPath">
        /// Path to the model.
        /// </param>
        public ModelReader(Func<string, Stream> modelLoader,
            string modelPath)
        {
            this.modelLoader = modelLoader;
            this.ModelPath = modelPath;
        }

        /// <summary>
        /// Loads the model into memory.
        /// </summary>
        public void LoadModel()
        {
            //Load model meta data
            LoadMetadata();

            //Load all feature set data
            LoadFeatureSet();

            //Load all features alpha data
            LoadFeatureWeights();
        }

        //get key feature id
        public virtual int get_id(string str)
        {
            return da.SearchByPerfectMatch(str);
        }

        public virtual double GetAlpha(long index)
        {
            return alpha_[index];
        }

        /// <summary>
        /// The default model loading strategy -
        /// load files from the file system.
        /// </summary>
        /// <param name="path">
        /// Model file path.</param>
        /// <returns>
        /// A stream containing the requested file.
        /// </returns>
        private static Stream GetStreamFromFileSystem(string path)
        {
            path.ThrowIfNotExists();
            return File.OpenRead(path);
        }

        /// <summary>
        /// Provides access to the metadata stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that points to the model metadata file.
        /// </returns>
        private Stream GetMetadataStream()
        {
            string path = ModelPath.ToMetadataModelName();

            return modelLoader(path);
        }

        /// <summary>
        /// Provides access to the feature set stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that allows accessing the model feature set file.
        /// </returns>
        private Stream GetFeatureSetStream()
        {
            string path = ModelPath.ToFeatureSetFileName();

            return modelLoader(path);
        }

        /// <summary>
        /// Provides access to the feature set stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that allows accessing the model feature weight file.
        /// </returns>
        private Stream GetFeatureWeightStream()
        {
            string path = ModelPath.ToFeatureWeightFileName();

            return modelLoader(path);
        }

        private void LoadMetadata()
        {
            using (Stream metadataStream = GetMetadataStream())
            {
                var sr = new StreamReader(metadataStream);
                string strLine;

                strLine = sr.ReadLine();
                version = uint.Parse(strLine.Split(':')[1].Trim());

                strLine = sr.ReadLine();
                cost_factor_ = double.Parse(strLine.Split(':')[1].Trim());

                strLine = sr.ReadLine();
                maxid_ = long.Parse(strLine.Split(':')[1].Trim());

                strLine = sr.ReadLine();
                xsize_ = uint.Parse(strLine.Split(':')[1].Trim());

                strLine = sr.ReadLine();

                y_ = new List<string>();
                while (true)
                {
                    strLine = sr.ReadLine();
                    if (strLine.Length == 0)
                    {
                        break;
                    }
                    y_.Add(strLine);
                }

                // load unigram and bigram template
                unigram_templs_ = new List<string>();
                bigram_templs_ = new List<string>();
                while (sr.EndOfStream == false)
                {
                    strLine = sr.ReadLine();
                    if (strLine.Length == 0)
                    {
                        break;
                    }
                    if (strLine[0] == 'U')
                    {
                        unigram_templs_.Add(strLine);
                    }
                    if (strLine[0] == 'B')
                    {
                        bigram_templs_.Add(strLine);
                    }
                }
                sr.Close();
            }
        }

        private void LoadFeatureSet()
        {
            Stream featureSetStream = GetFeatureSetStream();
            da = new DoubleArrayTrieSearch();
            da.Load(featureSetStream);
        }

        private void LoadFeatureWeights()
        {
            //feature weight array
            alpha_ = new double[maxid_ + 1];

            using (Stream featureWeightStream = GetFeatureWeightStream())
            {
                //Load all features alpha data
                var sr_alpha = new StreamReader(featureWeightStream);
                var br_alpha = new BinaryReader(sr_alpha.BaseStream);

                //Get VQ Size
                int vqSize = br_alpha.ReadInt32();

                if (vqSize > 0)
                {
                    //This is a VQ model, we need to get code book at first
                    List<double> vqCodeBook = new List<double>();
                    for (int i = 0; i < vqSize; i++)
                    {
                        vqCodeBook.Add(br_alpha.ReadDouble());
                    }

                    //Load weights
                    for (long i = 0; i < maxid_; i++)
                    {
                        int vqIdx = br_alpha.ReadByte();
                        alpha_[i] = vqCodeBook[vqIdx];
                    }
                }
                else
                {
                    //This is a normal model
                    for (long i = 0; i < maxid_; i++)
                    {
                        alpha_[i] = br_alpha.ReadSingle();
                    }
                }

                br_alpha.Close();
            }
        }

    }
}
