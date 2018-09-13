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
        public string TransformFilePath { get; set; }
        public RangeTransform Transform { get; set; }

        /// <summary>
        /// Feature dimension
        /// </summary>
        public int Dimension { get; set; }
    }
}
