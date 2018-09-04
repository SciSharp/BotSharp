using SVM.BotSharp.MachineLearning;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public class ClassifyOptions
    {
        public string TrainingCorpusDir { get; set; }
        public string ModelFilePath { get; set; }
        public Model Model { get; set; }
        public string PrediceOutputFile { get; set; }
    }
}
