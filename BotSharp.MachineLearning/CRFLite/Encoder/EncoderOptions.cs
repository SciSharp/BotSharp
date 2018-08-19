using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Encoder
{
    public class EncoderOptions
    {
        /// <summary>
        /// Maximum iteration
        /// </summary>
        public int MaxIteration { get; set; }

        /// <summary>
        /// Minimum feature frequency, if one feature's frequency is less than this value, the feature will be dropped.
        /// </summary>
        public int MinFeatureFreq = 2;

        /// <summary>
        /// Minimum diff value, when diff less than the value consecutive 3 times, the process will be ended.
        /// </summary>
        public double MinDifference;

        /// <summary>
        /// The maximum slot usage rate threshold when building feature set.
        /// </summary>
        public double SlotUsageRateThreshold { get; set; }

        /// <summary>
        /// The amount of threads used to train model.
        /// </summary>
        public int ThreadsNum { get; set; }

        /// <summary>
        /// Regularization type
        /// </summary>
        public CRFEncoder.REG_TYPE RegType { get; set; }

        /// <summary>
        /// Template file name
        /// </summary>
        [Required]
        public string TemplateFileName { get; set; }

        /// <summary>
        /// Training corpus file name
        /// </summary>
        [Required]
        public string TrainingCorpusFileName { get; set; }

        /// <summary>
        /// Encoded model file name
        /// </summary>
        [Required]
        public string ModelFileName { get; set; }

        /// <summary>
        /// The model file name for re-training
        /// </summary>
        public string RetrainModelFileName { get; set; }

        /// <summary>
        /// Debug level
        /// </summary>
        public int DebugLevel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint HugeLexMemLoad { get; set; }

        /// <summary>
        /// cost factor, too big or small value may lead encoded model over tune or under tune
        /// </summary>
        public double CostFactor { get; set; }

        /// <summary>
        /// If we build vector quantization model for feature weights
        /// </summary>
        public bool BVQ { get; set; }

        public EncoderOptions()
        {
            MaxIteration = 100;
            MinFeatureFreq = 2;
            MinDifference = 0.0001;
            SlotUsageRateThreshold = 0.95;
            ThreadsNum = 1;
            RegType = CRFEncoder.REG_TYPE.L2;
            CostFactor = 1.0;
        }
    }
}
