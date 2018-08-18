using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Decoder
{
    public class DecoderOptions
    {
        [Required]
        public string strModelFileName;
        [Required]
        public string strInputFileName;
        public string strOutputFileName;
        public string strOutputSegFileName;
        public int nBest;
        public int thread;
        public int probLevel;
        public int maxword;

        public DecoderOptions()
        {
            thread = 1;
            nBest = 1;
            probLevel = 0;
            maxword = 100;
        }
    }
}
