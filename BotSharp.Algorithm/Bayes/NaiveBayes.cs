using BotSharp.Algorithm.Estimators;
using BotSharp.Algorithm.Features;
using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Bayes
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Bayes%27_theorem
    /// </summary>
    public class NaiveBayes<Estimator> where Estimator : IEstimator, new()
    {
        /// <summary>
        /// smoothing function
        /// </summary>
        private Estimator estomator;

        public List<FeaturesDistribution> FeaturesDist { get; set; }

        public List<Probability> LabelDist { get; set; }

        public NaiveBayes()
        {
            estomator = new Estimator();
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
            prob = Math.Log(estomator.Prob(LabelDist, Y), 2);

            // posterior probability P(X1,...,Xn|Y) = Sum(P(X1|Y) +...+ P(Xn|Y)
            var featuresIfY = FeaturesDist.Where(fd => fd.Label == Y).ToList();

            // loop features
            for (int x = 0; x < features.Count; x++)
            {
                var Xn = features[x];
                var fv = featuresIfY.FirstOrDefault(fd => fd.FeatureName == Xn.Name)?.FeatureValues;

                if(fv != null)
                {
                    // features are independent, so calculate every feature prob and sum them
                    prob += Math.Log(estomator.Prob(fv, Xn.Value), 2);
                }
            }

            return prob;
        }
    }
}
