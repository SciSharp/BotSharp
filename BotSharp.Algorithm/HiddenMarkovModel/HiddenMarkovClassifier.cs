using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovClassifier 
    {
        protected int mClassCount;
        protected int mSymbolCount;

        protected HiddenMarkovModel[] mModels;
        protected HiddenMarkovModel mThreshold;
        protected double mWeight;
        protected double[] mClassPriors;


        /// <summary>
        ///   Gets the prior distribution assumed for the classes.
        /// </summary>
        /// 
        public double[] Priors
        {
            get { return mClassPriors; }
        }

        public int ClassCount
        {
            get { return mClassCount; }
        }

        public int SymbolCount
        {
            get { return mSymbolCount; }
        }

        public HiddenMarkovModel Threshold
        {
            get { return mThreshold; }
            set { mThreshold = value; }
        }

        public HiddenMarkovClassifier(int class_count, int[] state_count_array, int symbol_count)
        {
            mClassCount = class_count;
            mSymbolCount = symbol_count;

            DiagnosticsHelper.Assert(state_count_array.Length >= class_count);

            mModels = new HiddenMarkovModel[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(state_count_array[i], symbol_count);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        public HiddenMarkovClassifier(int class_count, ITopology topology, int symbol_count)
        {
            mClassCount = class_count;
            mSymbolCount = symbol_count;

            mModels = new HiddenMarkovModel[mClassCount];

            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(topology, symbol_count);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        public HiddenMarkovClassifier(int class_count, ITopology[] topology_array, int symbol_count)
        {
            mClassCount = class_count;
            mSymbolCount = symbol_count;

            DiagnosticsHelper.Assert(topology_array.Length >= class_count);

            mModels = new HiddenMarkovModel[mClassCount];

            for (int i = 0; i < mClassCount; ++i)
            {
                HiddenMarkovModel hmm = new HiddenMarkovModel(topology_array[i], symbol_count);
                mModels[i] = hmm;
            }

            mClassPriors = new double[mClassCount];
            for (int i = 0; i < mClassCount; ++i)
            {
                mClassPriors[i] = 1.0 / mClassCount;
            }
        }

        protected double LogLikelihood(int[] sequence)
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

        public int Compute(int[] sequence)
        {
            double[] class_probabilities = null;
            return Compute(sequence, out class_probabilities);
        }

        public int Compute(int[] sequence, out double[] class_probabilities)
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
            for(int i=0; i < mClassCount; ++i)
            {
                if(most_likely_model_probablity < logLikelihoods[i])
                {
                    most_likely_model_probablity=logLikelihoods[i];
                    most_likely_model_index=i;
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

        public HiddenMarkovModel[] Models
        {
            get { return mModels; }
            private set { }
        }
    }
}
