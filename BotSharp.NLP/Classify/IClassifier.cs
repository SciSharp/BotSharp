using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public interface IClassifier
    {
        void Train(List<LabeledFeatureSet> featureSets, ClassifyOptions options);

        void Classify(LabeledFeatureSet featureSet, ClassifyOptions options);
    }
}
