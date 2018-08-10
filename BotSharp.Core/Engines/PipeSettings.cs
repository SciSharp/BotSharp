using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class PipeSettings
    {
        public string TrainDir { get; set; }
        public string ModelDir { get; set; }
        public string AlgorithmDir { get; set; }
        public string PredictDir { get; set; }
    }
}
