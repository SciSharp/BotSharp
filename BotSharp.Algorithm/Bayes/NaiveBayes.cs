using BotSharp.Algorithm.Extensions;
using BotSharp.Algorithm.Formulas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Bayes
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Bayes%27_theorem
    /// </summary>
    public class NaiveBayes
    {
        /// <summary>
        /// smoothing function
        /// </summary>
        private Lidstone smoother;

        public List<FeatureFrequencyDistribution> FeatureDist { get; set; }

        public List<Probability> LabelDist { get; set; }

        public NaiveBayes()
        {
            smoother = new Lidstone();
        }

        /// <summary>
        /// calculate posterior probability P(Y|X)
        /// X is feature set, Y is label
        /// P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
        /// P(X, Y) = P(Y|X)P(X) = P(X|Y)P(Y) => P(Y|X) = P(Y)P(X|Y)/P(X)
        /// </summary>
        /// <param name="Y">label</param>
        /// <param name="featureSet"></param>
        /// <returns></returns>
        public double PosteriorProb(string Y, LabeledFeatureSet featureSet)
        {
            double prob = 0;

            // prior probability
            prob = smoother.Log2Prob(LabelDist, Y);

            // posterior probability P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
            featureSet.Features.ForEach(f =>
            {
                var fv = FeatureDist.Find(x => x.Label == Y && x.FeatureName == f.Name).FeatureValues;
                prob += smoother.Log2Prob(fv, f.Value);
            });

            return prob;
        }
    }

    public class Feature
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Feature(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class FeatureFrequencyDistribution
    {
        public string Label { get; set; }

        public string FeatureName { get; set; }

        public List<Probability> FeatureValues { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {FeatureValues.Count}";
        }
    }

    public class LabeledFeatureSet
    {
        public List<Feature> Features { get; set; }
        public string Label { get; set; }
        public LabeledFeatureSet()
        {
            this.Features = new List<Feature>();
        }
    }
}
