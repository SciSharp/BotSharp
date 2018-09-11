using BotSharp.Algorithm.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public interface IClassifier
    {
        void Train(List<FeaturesWithLabel> featureSets, ClassifyOptions options);

        List<Tuple<string, double>> Classify(List<Feature> features, ClassifyOptions options);

        /// <summary>
        /// Training by feature vector
        /// </summary>
        /// <param name="featureSets"></param>
        /// <param name="options"></param>
        void Train(List<Tuple<string, double[]>> featureSets, ClassifyOptions options);

        /// <summary>
        /// Predict by feature vector
        /// </summary>
        /// <param name="features"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        List<Tuple<string, double>> Classify(double[] features, ClassifyOptions options);
    }
}
