using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using BotSharp.Algorithm.HiddenMarkovModel.Topology;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovModel
    {
        protected DistributionModel[] mEmissionModels;

        protected int mDimension = 1;
        protected bool mMultivariate;

        public int Dimension
        {
            get { return mDimension; }
        }

        public DistributionModel[] EmissionModels
        {
            get { return mEmissionModels; }
        }

        public HiddenMarkovModel(ITopology topology, DistributionModel emissions)
        {
            mStateCount = topology.Create(out mLogTransitionMatrix, out mLogProbabilityVector);

            mEmissionModels = new DistributionModel[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                mEmissionModels[i] = emissions.Clone();
            }

            if (emissions is MultivariateDistributionModel)
            {
                mMultivariate = true;
                mDimension = ((MultivariateDistributionModel)mEmissionModels[0]).Dimension;
            }
        }

        public HiddenMarkovModel(ITopology topology, DistributionModel[] emissions)
        {
            mStateCount = topology.Create(out mLogTransitionMatrix, out mLogProbabilityVector);
            DiagnosticsHelper.Assert(emissions.Length == mStateCount);

            mEmissionModels = new DistributionModel[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                mEmissionModels[i] = emissions[i].Clone();
            }

            if (emissions[0] is MultivariateDistributionModel)
            {
                mMultivariate = true;
                mDimension = ((MultivariateDistributionModel)mEmissionModels[0]).Dimension;
            }
        }

        public HiddenMarkovModel(double[,] A, DistributionModel[] emissions, double[] pi)
        {
            mStateCount = mLogProbabilityVector.Length;
            DiagnosticsHelper.Assert(emissions.Length == mStateCount);

            mLogTransitionMatrix = LogHelper.Log(A);
            mLogProbabilityVector = LogHelper.Log(pi);

            mEmissionModels = new DistributionModel[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                mEmissionModels[i] = emissions[i].Clone();
            }

            if (emissions[0] is MultivariateDistributionModel)
            {
                mMultivariate = true;
                mDimension = ((MultivariateDistributionModel)mEmissionModels[0]).Dimension;
            }
        }

        public HiddenMarkovModel(int state_count, DistributionModel emissions)
        {
            mStateCount = state_count;

            mLogTransitionMatrix = new double[mStateCount, mStateCount];
            mLogProbabilityVector = new double[mStateCount];

            mLogProbabilityVector[0] = 1.0;

            for (int i = 0; i < mStateCount; ++i)
            {
                mLogProbabilityVector[i] = System.Math.Log(mLogProbabilityVector[i]);

                for (int j = 0; j < mStateCount; ++j)
                {
                    mLogTransitionMatrix[i, j] = System.Math.Log(1.0 / mStateCount);
                }
            }

            mEmissionModels = new DistributionModel[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                mEmissionModels[i] = emissions.Clone();
            }

            if (emissions is MultivariateDistributionModel)
            {
                mMultivariate = true;
                mDimension = ((MultivariateDistributionModel)mEmissionModels[0]).Dimension;
            }
        }

        public HiddenMarkovModel(int state_count, DistributionModel[] emissions)
        {
            mStateCount = state_count;
            DiagnosticsHelper.Assert(emissions.Length == mStateCount);
            
            mLogTransitionMatrix = new double[mStateCount, mStateCount];
            mLogProbabilityVector = new double[mStateCount];

            mLogProbabilityVector[0] = 1.0;

            for (int i = 0; i < mStateCount; ++i)
            {
                mLogProbabilityVector[i] = System.Math.Log(mLogProbabilityVector[i]);

                for (int j = 0; j < mStateCount; ++j)
                {
                    mLogTransitionMatrix[i, j] = System.Math.Log(1.0 / mStateCount);
                }
            }

            mEmissionModels = new DistributionModel[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                mEmissionModels[i] = emissions[0].Clone();
            }

            if (emissions[0] is MultivariateDistributionModel)
            {
                mMultivariate = true;
                mDimension = ((MultivariateDistributionModel)mEmissionModels[0]).Dimension;
            }
        }

        public double Evaluate(double[] sequence)
        {
            double logLikelihood;
            ForwardBackwardAlgorithm.LogForward(mLogTransitionMatrix, mEmissionModels, mLogProbabilityVector, sequence, out logLikelihood);

            return logLikelihood;
        }

        public int[] Decode(double[] sequence, out double logLikelihood)
        {
            return Viterbi.LogForward(mLogTransitionMatrix, mEmissionModels, mLogProbabilityVector, sequence, out logLikelihood);
        }

        public int[] Decode(double[] sequence)
        {
            double logLikelihood;
            return Decode(sequence, out logLikelihood);
        }
    }
}
