using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Models.CRFLite.Decoder
{
    public class DecoderOptions
    {
        /// <summary>
        /// Model file path
        /// </summary>
        public string ModelFileName;

        /// <summary>
        /// 
        /// </summary>
        public string InputFileName;

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
        /// Max words length in one sentence
        /// </summary>
        public int MaxWord;

        public DecoderOptions()
        {
            Thread = 1;
            NBest = 2;
            ProbLevel = 0;
            MaxWord = 128;
        }
    }
}
