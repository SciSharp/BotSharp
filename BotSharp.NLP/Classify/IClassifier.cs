using Bigtree.Algorithm.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public interface IClassifier
    {
        /// <summary>
        /// Training by feature vector
        /// </summary>
        /// <param name="sentences"></param>
        /// <param name="options"></param>
        void Train(List<Sentence> sentences, ClassifyOptions options);

        /// <summary>
        /// Predict by feature vector
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        List<Tuple<string, double>> Classify(Sentence sentence, ClassifyOptions options);

        String SaveModel(ClassifyOptions options);

        Object LoadModel(ClassifyOptions options);
    }
}
