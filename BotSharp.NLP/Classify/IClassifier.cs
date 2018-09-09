using BotSharp.Algorithm.Bayes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public interface IClassifier
    {
        void Train(List<FeaturesWithLabel> featureSets, ClassifyOptions options);

        List<Tuple<string, double>> Classify(List<Feature> features, ClassifyOptions options);
    }
}
