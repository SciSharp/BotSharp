using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovModel
    {
        public int[] Generate(int samples)
        {
            int[] path; 
            double logLikelihood;
            return Generate(samples, out path, out logLikelihood);
        }

        /// <summary>
        ///   Generates a random vector of observations from the model.
        /// </summary>
        /// 
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="logLikelihood">The log-likelihood of the generated observation sequence.</param>
        /// <param name="path">The Viterbi path of the generated observation sequence.</param>
        /// 
        /// <example>
        ///   An usage example is available at the <see cref="Generate(int)"/> documentation page.
        /// </example>
        /// 
        /// <returns>A random vector of observations drawn from the model.</returns>
        /// 
        public int[] Generate(int samples, out int[] path, out double logLikelihood)
        {
            double[] transitions = mLogProbabilityVector;
            double[] emissions;

            int[] observations = new int[samples];
            logLikelihood = Double.NegativeInfinity;
            path = new int[samples];


            // For each observation to be generated
            for (int t = 0; t < observations.Length; t++)
            {
                // Navigate randomly on one of the state transitions
                int state = MathHelper.Random(LogHelper.Exp(transitions));

                // Generate a sample for the state
                emissions = MathHelper.GetRow(mLogEmissionMatrix, state);

                int symbol = MathHelper.Random(LogHelper.Exp(emissions));

                // Store the sample
                observations[t] = symbol;
                path[t] = state;

                // Compute log-likelihood up to this point
                logLikelihood = LogHelper.LogSum(logLikelihood, transitions[state] + emissions[symbol]);

                // Continue sampling
                transitions = MathHelper.GetRow(mLogTransitionMatrix, state);
            }

            return observations;
        }
    }
}
