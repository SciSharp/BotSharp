using Bigtree.Algorithm.SVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public class ClassifyOptions
    {
        public string TrainingCorpusDir { get; set; }
        public string ModelFilePath { get; set; }
        public string ModelDir { get; set; }
        public string ModelName { get; set; }

        public string FeaturesFileName { get; set; }
        public string FeaturesInTfIdfFileName { get; set; }
        public string DictionaryFileName { get; set; }
        public string CategoriesFileName { get; set; }

        public string PrediceOutputFile { get; set; }
        public string TransformFilePath { get; set; }
        public RangeTransform Transform { get; set; }

        /// <summary>
        /// Feature dimension
        /// </summary>
        public int Dimension { get; set; }
    }
}
