using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;

namespace BotSharp.Algorithm.HiddenMarkovModel.Topology
{
    public class Ergodic : ITopology
    {
        protected int mStateCount = 0;
        protected bool mRandom = false;

        public Ergodic(int state_count, bool random=false)
        {
            mStateCount = state_count;
            mRandom = random;
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
                    for (int j = 0; j < mStateCount; ++j)
                    {
                        sum += logTransitionMatrix[i, j] = MathHelper.NextDouble();
                    }
                    for (int j = 0; j < mStateCount; ++j)
                    {
                        double transition_value = logTransitionMatrix[i, j];
                        logTransitionMatrix[i, j] = System.Math.Log(transition_value / sum);
                    }
                }
            }
            else
            {
                for (int i = 0; i < mStateCount; ++i)
                {
                    for (int j = 0; j < mStateCount; ++j)
                    {
                        logTransitionMatrix[i, j] = System.Math.Log(1.0 / mStateCount);
                    }
                }
            }

            return mStateCount;
        }
    }
}
