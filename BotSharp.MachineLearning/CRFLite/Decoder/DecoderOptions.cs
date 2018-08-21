using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Decoder
{
    public class DecoderOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public string ModelFileName;

        /// <summary>
        /// 
        /// </summary>
        public string InputFileName;

        /// <summary>
        /// 
        /// </summary>
        public string OutputFileName;

        /// <summary>
        /// 
        /// </summary>
        public string OutputSegFileName;

        /// <summary>
        /// 
        /// </summary>
        public int NBest;

        /// <summary>
        /// 
        /// </summary>
        public int Thread;

        /// <summary>
        /// 
        /// </summary>
        public int ProbLevel;

        /// <summary>
        /// 
        /// </summary>
        public int MaxWord;

        public DecoderOptions()
        {
            Thread = 1;
            NBest = 1;
            ProbLevel = 0;
            MaxWord = 100;
        }
    }
}
