using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Topology
{
    public class Forward : ITopology
    {
        protected int mStateCount;
        protected int mDeepness;
        protected bool mRandom;

        public Forward(int state_count, int deepness, bool random = false)
        {
            mStateCount = state_count;
            mDeepness = deepness;
            mRandom = random;
        }

        public Forward(int state_count, bool random = false)
            : this(state_count, state_count, random)
        {

        }

        public int Create(out double[,] logTransitionMatrix, out double[] logInitialState)
        {
            logTransitionMatrix = new double[mStateCount, mStateCount];
            logInitialState = new double[mStateCount];

            for (int i = 0; i < mStateCount; ++i)
            {
                logInitialState[i] = double.NegativeInfinity;
            }
            logInitialState[0] = 0.0;

            if (mRandom)
            {
                for (int i = 0; i < mStateCount; ++i)
                {
                    double sum = 0.0;
                    for (int j = i; j < mDeepness; ++j)
                    {
                        sum += logTransitionMatrix[i, j] = MathHelper.NextDouble();
                    }
                    for (int j = i; j < mDeepness; ++j)
                    {
                        double transition_value = logTransitionMatrix[i, j];
                        logTransitionMatrix[i, j] = transition_value / sum;
                    }
                }
            }
            else
            {
                for (int i = 0; i < mStateCount; ++i)
                {
                    double sum = System.Math.Min(mDeepness, mStateCount - i);
                    for (int j = i; j < mStateCount && (j-i) < mDeepness; ++j)
                    {
                        logTransitionMatrix[i, j] = 1.0 / sum;
                    }
                }
            }

            for (int i = 0; i < mStateCount; ++i)
            {
                for (int j = 0; j < mStateCount; ++j)
                {
                    logTransitionMatrix[i, j] = System.Math.Log(logTransitionMatrix[i, j]);
                }
            }

            return mStateCount;
        }
    }
}
