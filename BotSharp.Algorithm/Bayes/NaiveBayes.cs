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
    public class NaiveBayes<Smoother> where Smoother : ISmoother, new()
    {
        /// <summary>
        /// smoothing function
        /// </summary>
        private Smoother smoother;

        public List<FeaturesDistribution> FeaturesDist { get; set; }

        public List<Probability> LabelDist { get; set; }

        public NaiveBayes()
        {
            smoother = new Smoother();
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
        public double PosteriorProb(string Y, List<Feature> features)
        {
            double prob = 0;

            // prior probability
            prob = Math.Log(smoother.Prob(LabelDist, Y), 2);

            // posterior probability P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
            var featuresIfY = FeaturesDist.Where(fd => fd.Label == Y).ToList();

            // loop features
            for (int x = 0; x < features.Count; x++)
            {
                var Xn = features[x];
                var fv = featuresIfY.First(fd => fd.FeatureName == Xn.Name).FeatureValues;

                // features are independent, so calculate every feature prob and sum them
                prob += Math.Log(smoother.Prob(fv, Xn.Value), 2);
            }

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

    public class FeaturesWithLabel
    {
        public List<Feature> Features { get; set; }
        public string Label { get; set; }
        public FeaturesWithLabel()
        {
            this.Features = new List<Feature>();
        }
    }

    public class FeaturesDistribution
    {
        public string Label { get; set; }

        public string FeatureName { get; set; }

        public List<Probability> FeatureValues { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {FeatureValues.Count}";
        }
    }
}
