using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.Topology;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class HiddenMarkovModel
    {
        protected double[,] mLogTransitionMatrix;
        protected double[,] mLogEmissionMatrix;
        protected double[] mLogProbabilityVector;
        protected int mSymbolCount = 0;
        protected int mStateCount = 0;

        public double[,] LogTransitionMatrix
        {
            get { return mLogTransitionMatrix; }
        }

        public double[,] LogEmissionMatrix
        {
            get { return mLogEmissionMatrix; }
        }

        public double[] LogProbabilityVector
        {
            get { return mLogProbabilityVector; }
        }

        public double[,] TransitionMatrix
        {
            get { return LogHelper.Exp(mLogTransitionMatrix); }
        }

        public double[,] EmissionMatrix
        {
            get { return LogHelper.Exp(mLogEmissionMatrix); }
        }

        public double[] ProbabilityVector
        {
            get { return LogHelper.Exp(mLogProbabilityVector); }
        }

        /// <summary>
        /// The number of states in the hidden Markov model
        /// </summary>
        public int StateCount
        {
            get { return mStateCount; }
        }

        /// <summary>
        /// The size of symbol set used to construct any observation from this model
        /// </summary>
        public int SymbolCount
        {
            get { return mSymbolCount; }
        }

        public HiddenMarkovModel(double[,] A, double[,] B, double[] pi)
        {
            mLogTransitionMatrix = LogHelper.Log(A);
            mLogEmissionMatrix = LogHelper.Log(B);
            mLogProbabilityVector = LogHelper.Log(pi);

            mStateCount = mLogProbabilityVector.Length;
            mSymbolCount = mLogEmissionMatrix.GetLength(1);
        }

        public HiddenMarkovModel(ITopology topology, int symbol_count)
        {
            mSymbolCount = symbol_count;
            mStateCount = topology.Create(out mLogTransitionMatrix, out mLogProbabilityVector);

            mLogEmissionMatrix = new double[mStateCount, mSymbolCount];

            for (int i = 0; i < mStateCount; i++)
            {
                for (int j = 0; j < mSymbolCount; j++)
                    mLogEmissionMatrix[i, j] = System.Math.Log(1.0 / mSymbolCount);
            }
        }

        public HiddenMarkovModel(int state_count, int symbol_count)
        {
            mStateCount = state_count;
            mSymbolCount = symbol_count;

            mLogTransitionMatrix = new double[mStateCount, mStateCount];
            mLogProbabilityVector = new double[mStateCount];
            mLogEmissionMatrix = new double[mStateCount, mSymbolCount];

            mLogProbabilityVector[0] = 1.0;

            for (int i = 0; i < mStateCount; ++i)
            {
                mLogProbabilityVector[i] = System.Math.Log(mLogProbabilityVector[i]);

                for (int j = 0; j < mStateCount; ++j)
                {
                    mLogTransitionMatrix[i, j] = System.Math.Log(1.0 / mStateCount);
                }
            }

            for (int i = 0; i < mStateCount; i++)
            {
                for (int j = 0; j < mSymbolCount; j++)
                    mLogEmissionMatrix[i, j] = System.Math.Log(1.0 / mSymbolCount);
            }
        }

        public double Evaluate(int[] sequence)
        {
            double logLikelihood;
            ForwardBackwardAlgorithm.LogForward(mLogTransitionMatrix, mLogEmissionMatrix, mLogProbabilityVector, sequence, out logLikelihood);

            return logLikelihood;
        }

        public int[] Decode(int[] sequence, out double logLikelihood)
        {
            return Viterbi.LogForward(mLogTransitionMatrix, mLogEmissionMatrix, mLogProbabilityVector, sequence, out logLikelihood);
        }

        public int[] Decode(int[] sequence)
        {
            double logLikelihood;
            return Decode(sequence, out logLikelihood);
        }
    }
}
