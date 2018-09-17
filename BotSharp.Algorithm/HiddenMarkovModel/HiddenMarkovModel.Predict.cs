using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovModel
    {
        /// <summary>
        ///   Predicts next observations occurring after a given observation sequence.
        /// </summary>
        public int[] Predict(int[] observations, int next, out double logLikelihood)
        {
            double[][] logLikelihoods;
            return Predict(observations, next, out logLikelihood, out logLikelihoods);
        }

        /// <summary>
        ///   Predicts next observations occurring after a given observation sequence.
        /// </summary>
        public int[] Predict(int[] observations, int next)
        {
            double logLikelihood;
            double[][] logLikelihoods;
            return Predict(observations, next, out logLikelihood, out logLikelihoods);
        }

        /// <summary>
        ///   Predicts next observations occurring after a given observation sequence.
        /// </summary>
        public int[] Predict(int[] observations, int next, out double[][] logLikelihoods)
        {
            double logLikelihood;
            return Predict(observations, next, out logLikelihood, out logLikelihoods);
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        public int Predict(int[] observations, out double[] probabilities)
        {
            double[][] logLikelihoods;
            double logLikelihood;
            int prediction = Predict(observations, 1, out logLikelihood, out logLikelihoods)[0];
            probabilities = logLikelihoods[0];
            return prediction;
        }

        /// <summary>
        ///   Predicts the next observations occurring after a given observation sequence (using Viterbi algorithm)
        /// </summary>
        public int[] Predict(int[] observations, int next, out double logLikelihood, out double[][] logLikelihoods)
        {
            int T = next;
            double[,] logA = LogTransitionMatrix;
            double[,] logB = LogEmissionMatrix;
            double[] logPi = LogProbabilityVector;

            int[] prediction = new int[next];
            logLikelihoods = new double[next][];


            // Compute forward probabilities for the given observation sequence.
            double[,] lnFw0 = ForwardBackwardAlgorithm.LogForward(logA, logB, logPi, observations, out logLikelihood);

            // Create a matrix to store the future probabilities for the prediction
            // sequence and copy the latest forward probabilities on its first row.
            double[,] lnFwd = new double[T + 1, mStateCount];


            // 1. Initialization
            for (int i = 0; i < mStateCount; i++)
                lnFwd[0, i] = lnFw0[observations.Length - 1, i];

            // 2. Induction
            for (int t = 0; t < T; t++)
            {
                double[] weights = new double[mSymbolCount];
                for (int s = 0; s < mSymbolCount; s++)
                {
                    weights[s] = Double.NegativeInfinity;

                    for (int i = 0; i < mStateCount; i++)
                    {
                        double sum = Double.NegativeInfinity;
                        for (int j = 0; j < mStateCount; j++)
                            sum = LogHelper.LogSum(sum, lnFwd[t, j] + logA[j, i]);
                        lnFwd[t + 1, i] = sum + logB[i, s];

                        weights[s] = LogHelper.LogSum(weights[s], lnFwd[t + 1, i]);
                    }
                }

                double sumWeight = Double.NegativeInfinity;
                for (int i = 0; i < weights.Length; i++)
                    sumWeight = LogHelper.LogSum(sumWeight, weights[i]);
                for (int i = 0; i < weights.Length; i++)
                    weights[i] -= sumWeight;


                // Select most probable symbol
                double maxWeight = weights[0];
                prediction[t] = 0;
                for (int i = 1; i < weights.Length; i++)
                {
                    if (weights[i] > maxWeight)
                    {
                        maxWeight = weights[i];
                        prediction[t] = i;
                    }
                }

                // Recompute log-likelihood
                logLikelihoods[t] = weights;
                logLikelihood = maxWeight;
            }


            return prediction;
        }
    }
}
