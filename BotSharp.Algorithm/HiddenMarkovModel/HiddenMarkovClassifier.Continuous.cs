using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.Topology;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovClassifier
    {

        public HiddenMarkovClassifier(int class_count, int[] state_count_array, DistributionModel B_distribution)
        {
            mClassCount = class_count;
            mSymbolCount = -1;

            DiagnosticsHelper.Assert(state_count_array.Length >= class_count);

            mModels = new HiddenMarkovModel[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(state_count_array[i], B_distribution);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        public HiddenMarkovClassifier(int class_count, ITopology topology, DistributionModel B_distribution)
        {
            mClassCount = class_count;
            mSymbolCount = -1;

            mModels = new HiddenMarkovModel[mClassCount];

            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(topology, B_distribution);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        public HiddenMarkovClassifier(int class_count, ITopology[] topology_array, DistributionModel B_distribution)
        {
            mClassCount = class_count;
            mSymbolCount = -1;

            DiagnosticsHelper.Assert(topology_array.Length >= class_count);

            mModels = new HiddenMarkovModel[mClassCount];

            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(topology_array[i], B_distribution);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        protected double LogLikelihood(double[] sequence)
        {
            double sum = Double.NegativeInfinity;

            for (int i = 0; i < mModels.Length; i++)
            {
                double prior = System.Math.Log(mClassPriors[i]);
                double model = mModels[i].Evaluate(sequence);
                double result = LogHelper.LogSum(prior, model);

                sum = LogHelper.LogSum(sum, result);
            }

            return sum;
        }

        public int Compute(double[] sequence)
        {
            double[] class_probabilities = null;
            return Compute(sequence, out class_probabilities);
        }

        public int Compute(double[] sequence, out double logLikelihood)
        {
            double[] class_probabilities = null;
            int output = Compute(sequence, out class_probabilities);
            logLikelihood = LogLikelihood(sequence);
            return output;
        }

        public int Compute(double[] sequence, out double[] class_probabilities)
        {
            double[] logLikelihoods = new double[mModels.Length];
            double thresholdValue = Double.NegativeInfinity;


            Parallel.For(0, mModels.Length + 1, i =>
            {
                if (i < mModels.Length)
                {
                    logLikelihoods[i] = mModels[i].Evaluate(sequence);
                }
                else if (mThreshold != null)
                {
                    thresholdValue = mThreshold.Evaluate(sequence);
                }
            });

            double lnsum = Double.NegativeInfinity;
            for (int i = 0; i < mClassPriors.Length; i++)
            {
                logLikelihoods[i] = System.Math.Log(mClassPriors[i]) + logLikelihoods[i];
                lnsum = LogHelper.LogSum(lnsum, logLikelihoods[i]);
            }

            if (mThreshold != null)
            {
                thresholdValue = System.Math.Log(mWeight) + thresholdValue;
                lnsum = LogHelper.LogSum(lnsum, thresholdValue);
            }

            int most_likely_model_index = 0;
            double most_likely_model_probablity = double.NegativeInfinity;
            for (int i = 0; i < mClassCount; ++i)
            {
                if (most_likely_model_probablity < logLikelihoods[i])
                {
                    most_likely_model_probablity = logLikelihoods[i];
                    most_likely_model_index = i;
                }
            }

            if (lnsum != Double.NegativeInfinity)
            {
                for (int i = 0; i < logLikelihoods.Length; i++)
                    logLikelihoods[i] -= lnsum;
            }

            // Convert to probabilities
            class_probabilities = logLikelihoods;
            for (int i = 0; i < logLikelihoods.Length; i++)
            {
                class_probabilities[i] = System.Math.Exp(logLikelihoods[i]);
            }

            return (thresholdValue > most_likely_model_probablity) ? -1 : most_likely_model_index;
        }
    }
}
